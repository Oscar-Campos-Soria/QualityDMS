using MediatR;
using QualityDMS.Application.Common.Exceptions;
using QualityDMS.Domain.Common;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Documents.Commands.UpdateDocument;

public class UpdateDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IFileStorageService fileStorage,
    ICurrentUserService currentUser,
    IUnitOfWork uow) : IRequestHandler<UpdateDocumentCommand, Result>
{
    public async Task<Result> Handle(UpdateDocumentCommand cmd, CancellationToken ct)
    {
        var document = await documentRepository.GetByIdWithVersionsAsync(cmd.DocumentId, ct)
            ?? throw new NotFoundException(nameof(Document), cmd.DocumentId);

        if (document.Status != DocumentStatus.Draft && document.Status != DocumentStatus.Rejected)
            return Result.Failure("Solo documentos en borrador o rechazados pueden editarse.");

        document.Title = cmd.Title;
        document.Description = cmd.Description;
        document.CategoryId = cmd.CategoryId;
        document.DepartmentId = cmd.DepartmentId;
        document.WorkflowTemplateId = cmd.WorkflowTemplateId;
        document.NextReviewDate = cmd.NextReviewDate;
        document.UpdatedBy = currentUser.UserId;

        if (cmd.FileStream is not null && cmd.FileName is not null && cmd.ContentType is not null)
        {
            var (filePath, sizeBytes) = await fileStorage.UploadAsync(
                cmd.FileStream, cmd.FileName, cmd.ContentType, ct);

            var versionNumber = cmd.NewVersionNumber ?? IncrementVersion(document.Versions);
            var version = DocumentVersion.Create(
                document.DocumentId, versionNumber, filePath,
                cmd.FileName, sizeBytes, cmd.ContentType, currentUser.UserId, cmd.ChangeLog);
            document.AddVersion(version);
        }

        documentRepository.Update(document);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string IncrementVersion(IEnumerable<DocumentVersion> versions)
    {
        var latest = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
        if (latest is null) return "1.0";
        if (Version.TryParse(latest.VersionNumber, out var v))
            return $"{v.Major}.{v.Minor + 1}";
        return "1.1";
    }
}
