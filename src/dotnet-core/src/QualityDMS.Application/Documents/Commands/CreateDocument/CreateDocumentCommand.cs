using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Documents.Commands.CreateDocument;

public record CreateDocumentCommand(
    string Code,
    string Title,
    string? Description,
    int CategoryId,
    int DepartmentId,
    int? WorkflowTemplateId,
    DateTime? NextReviewDate,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream
) : IRequest<Result<int>>;
