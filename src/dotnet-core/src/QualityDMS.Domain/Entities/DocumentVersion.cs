using QualityDMS.Domain.Common;

namespace QualityDMS.Domain.Entities;

public class DocumentVersion : AuditableEntity
{
    public int VersionId { get; set; }
    public int DocumentId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? ChangeLog { get; set; }
    public bool IsCurrent { get; set; }

    public Document Document { get; set; } = null!;

    public static DocumentVersion Create(int documentId, string versionNumber, string filePath,
        string fileName, long fileSizeBytes, string contentType, string createdBy, string? changeLog = null)
    {
        return new DocumentVersion
        {
            DocumentId = documentId,
            VersionNumber = versionNumber,
            FilePath = filePath,
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            ChangeLog = changeLog,
            IsCurrent = true,
            CreatedBy = createdBy
        };
    }
}
