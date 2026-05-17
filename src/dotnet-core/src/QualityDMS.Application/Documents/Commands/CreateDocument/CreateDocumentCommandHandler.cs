using MediatR;
using QualityDMS.Domain.Common;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Documents.Commands.CreateDocument;

public class CreateDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IFileStorageService fileStorage,
    ICurrentUserService currentUser,
    IUnitOfWork uow) : IRequestHandler<CreateDocumentCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateDocumentCommand cmd, CancellationToken ct)
    {
        if (await documentRepository.CodeExistsAsync(cmd.Code, ct: ct))
            return Result.Failure<int>($"El código '{cmd.Code}' ya existe.");

        var (filePath, sizeBytes) = await fileStorage.UploadAsync(
            cmd.FileStream, cmd.FileName, cmd.ContentType, ct);

        var document = Document.Create(cmd.Code, cmd.Title, cmd.CategoryId, cmd.DepartmentId, currentUser.UserId);
        document.Description = cmd.Description;
        document.WorkflowTemplateId = cmd.WorkflowTemplateId;
        document.NextReviewDate = cmd.NextReviewDate;

        await documentRepository.AddAsync(document, ct);
        await uow.SaveChangesAsync(ct);

        var version = DocumentVersion.Create(
            document.DocumentId, "1.0", filePath,
            cmd.FileName, sizeBytes, cmd.ContentType, currentUser.UserId);
        document.AddVersion(version);

        documentRepository.Update(document);
        await uow.SaveChangesAsync(ct);

        return Result.Success(document.DocumentId);
    }
}
