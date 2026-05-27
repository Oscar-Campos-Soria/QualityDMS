using QualityDMS.Domain.Enums;

namespace QualityDMS.Application.Documents.DTOs;

public class DocumentDto
{
    public int DocumentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DocumentStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public string CategoryName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string? CurrentVersion { get; init; }
    public DateTime? NextReviewDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public class DocumentDetailDto : DocumentDto
{
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public int DepartmentId { get; init; }
    public int? WorkflowTemplateId { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public IEnumerable<DocumentVersionDto> Versions { get; init; } = Enumerable.Empty<DocumentVersionDto>();
}

public class DocumentVersionDto
{
    public int VersionId { get; init; }
    public string VersionNumber { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public long FileSizeBytes { get; init; }
    public string? ChangeLog { get; init; }
    public bool IsCurrent { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}
