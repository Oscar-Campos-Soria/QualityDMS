using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Documents.Commands.UpdateDocument;

public record UpdateDocumentCommand(
    int DocumentId,
    string Title,
    string? Description,
    int CategoryId,
    int DepartmentId,
    int? WorkflowTemplateId,
    DateTime? NextReviewDate,
    string? NewVersionNumber,
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    Stream? FileStream,
    string? ChangeLog
) : IRequest<Result>;
