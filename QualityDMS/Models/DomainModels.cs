using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace QualityDMS.Models
{
    // =========================================================================
    // Enumeraciones
    // =========================================================================
    public enum DocumentStatus : byte
    {
        Borrador = 1,
        EnRevision = 2,
        EnAprobacion = 3,
        Aprobado = 4,
        Obsoleto = 5,
        Cancelado = 6
    }

    public enum VersionType : byte
    {
        Mayor = 1,
        Menor = 2,
        Parche = 3
    }

    public enum WorkflowStepType : byte
    {
        Revision = 1,
        Aprobacion = 2,
        Notificacion = 3,
        FirmaDigital = 4
    }

    public enum WorkflowActionType : byte
    {
        Aprobado = 1,
        Rechazado = 2,
        SolicitoSambios = 3,
        Delegado = 4,
        Omitido = 5
    }

    public enum WorkflowInstanceStatus : byte
    {
        EnCurso = 1,
        Completado = 2,
        Rechazado = 3,
        Cancelado = 4
    }

    public enum AuditType : byte
    {
        Interna = 1,
        Externa = 2,
        DeProveedor = 3,
        DeCertificacion = 4
    }

    public enum FindingType : byte
    {
        NoConformidadMayor = 1,
        NoConformidadMenor = 2,
        Observacion = 3,
        OportunidadMejora = 4,
        PuntoFuerte = 5
    }

    public enum FindingStatus : byte
    {
        Abierta = 1,
        EnTratamiento = 2,
        Verificada = 3,
        Cerrada = 4
    }

    // =========================================================================
    // Entidades
    // =========================================================================

    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Position { get; set; }

        public string? Signature { get; set; }   // Base64

        public int? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Document> OwnedDocuments { get; set; } = [];
        public ICollection<DocumentVersion> AuthoredVersions { get; set; } = [];
    }

    public class Department : BaseEntity
    {
        public int DepartmentId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? ManagerName { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Document> Documents { get; set; } = [];
        public ICollection<ApplicationUser> Users { get; set; } = [];
    }

    public class DocumentCategory : BaseEntity
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public DocumentCategory? Parent { get; set; }

        public int RetentionYears { get; set; } = 5;
        public bool IsActive { get; set; } = true;

        public ICollection<DocumentCategory> Children { get; set; } = [];
        public ICollection<Document> Documents { get; set; } = [];
    }

    public class Document : BaseEntity
    {
        public int DocumentId { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public DocumentCategory Category { get; set; } = null!;

        public int DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department Department { get; set; } = null!;

        [Required]
        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public ApplicationUser Owner { get; set; } = null!;

        public DocumentStatus CurrentStatus { get; set; } = DocumentStatus.Borrador;

        public int? CurrentVersionId { get; set; }
        [ForeignKey(nameof(CurrentVersionId))]
        public DocumentVersion? CurrentVersionNav { get; set; }

        [MaxLength(20)]
        public string CurrentVersion { get; set; } = "0.1";

        public string? Tags { get; set; }   // JSON array
        public bool IsConfidential { get; set; } = false;
        public DateOnly? NextReviewDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        public ICollection<DocumentVersion> Versions { get; set; } = [];

        [NotMapped]
        public string StatusLabel => CurrentStatus switch
        {
            DocumentStatus.Borrador => "Borrador",
            DocumentStatus.EnRevision => "En Revisión",
            DocumentStatus.EnAprobacion => "En Aprobación",
            DocumentStatus.Aprobado => "Aprobado",
            DocumentStatus.Obsoleto => "Obsoleto",
            DocumentStatus.Cancelado => "Cancelado",
            _ => "Desconocido"
        };

        [NotMapped]
        public string StatusBadgeClass => CurrentStatus switch
        {
            DocumentStatus.Borrador => "badge bg-secondary",
            DocumentStatus.EnRevision => "badge bg-info text-dark",
            DocumentStatus.EnAprobacion => "badge bg-warning text-dark",
            DocumentStatus.Aprobado => "badge bg-success",
            DocumentStatus.Obsoleto => "badge bg-danger",
            DocumentStatus.Cancelado => "badge bg-dark",
            _ => "badge bg-light text-dark"
        };
    }

    public class DocumentVersion : BaseEntity
    {
        public int VersionId { get; set; }

        public int DocumentId { get; set; }
        [ForeignKey(nameof(DocumentId))]
        public Document Document { get; set; } = null!;

        [Required, MaxLength(20)]
        public string VersionNumber { get; set; } = string.Empty;

        public VersionType VersionType { get; set; } = VersionType.Menor;

        [MaxLength(2000)]
        public string? ChangeLog { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string FileExtension { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [Required, MaxLength(200)]
        public string ContentType { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string FileHash { get; set; } = string.Empty;

        public DocumentStatus Status { get; set; } = DocumentStatus.Borrador;
        public DateTime? PublishedAt { get; set; }
        public DateTime? ObsoletedAt { get; set; }
        public DateOnly? EffectiveDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser Author { get; set; } = null!;

        public string? ReviewedById { get; set; }
        [ForeignKey(nameof(ReviewedById))]
        public ApplicationUser? ReviewedBy { get; set; }

        public string? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(2000)]
        public string? Comments { get; set; }

        [NotMapped]
        public string FileSizeFormatted => FileSizeBytes switch
        {
            < 1024 => $"{FileSizeBytes} B",
            < 1048576 => $"{FileSizeBytes / 1024.0:F1} KB",
            < 1073741824 => $"{FileSizeBytes / 1048576.0:F1} MB",
            _ => $"{FileSizeBytes / 1073741824.0:F1} GB"
        };

        [NotMapped]
        public string FileIconClass => FileExtension.ToLower() switch
        {
            ".pdf" => "bi bi-file-earmark-pdf text-danger",
            ".docx" or ".doc" => "bi bi-file-earmark-word text-primary",
            ".xlsx" or ".xls" => "bi bi-file-earmark-excel text-success",
            ".pptx" or ".ppt" => "bi bi-file-earmark-ppt text-warning",
            ".dwg" or ".dxf" => "bi bi-file-earmark-code text-info",
            ".png" or ".jpg" or ".jpeg" => "bi bi-file-earmark-image text-secondary",
            _ => "bi bi-file-earmark text-muted"
        };
    }

    public class WorkflowTemplate : BaseEntity
    {
        [Key]
        public int TemplateId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public DocumentCategory? Category { get; set; }

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public string CreatedBy { get; set; } = string.Empty;
        [ForeignKey(nameof(CreatedBy))]
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<WorkflowTemplateStep> Steps { get; set; } = [];
    }

    public class WorkflowTemplateStep
    {
        [Key]
        public int StepId { get; set; }
        public int TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public WorkflowTemplate Template { get; set; } = null!;

        public int StepOrder { get; set; }

        [Required, MaxLength(150)]
        public string StepName { get; set; } = string.Empty;

        public WorkflowStepType StepType { get; set; }

        [MaxLength(256)]
        public string? RoleRequired { get; set; }

        public string? AssigneeId { get; set; }
        [ForeignKey(nameof(AssigneeId))]
        public ApplicationUser? Assignee { get; set; }

        public int DaysAllowed { get; set; } = 3;
        public bool IsRequired { get; set; } = true;
    }

    public class WorkflowInstance
    {
        [Key]
        public int InstanceId { get; set; }

        public int VersionId { get; set; }
        [ForeignKey(nameof(VersionId))]
        public DocumentVersion Version { get; set; } = null!;

        public int? TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public WorkflowTemplate? Template { get; set; }

        public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.EnCurso;
        public int CurrentStep { get; set; } = 1;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public string InitiatedBy { get; set; } = string.Empty;
        [ForeignKey(nameof(InitiatedBy))]
        public ApplicationUser InitiatedByUser { get; set; } = null!;

        public ICollection<WorkflowAction> Actions { get; set; } = [];
    }

    public class WorkflowAction
    {
        [Key]
        public int ActionId { get; set; }
        public int InstanceId { get; set; }
        [ForeignKey(nameof(InstanceId))]
        public WorkflowInstance Instance { get; set; } = null!;

        public int StepOrder { get; set; }

        [MaxLength(150)]
        public string StepName { get; set; } = string.Empty;

        public WorkflowStepType StepType { get; set; }
        public WorkflowActionType ActionType { get; set; }

        public string ActorId { get; set; } = string.Empty;
        [ForeignKey(nameof(ActorId))]
        public ApplicationUser Actor { get; set; } = null!;

        [MaxLength(2000)]
        public string? Comments { get; set; }

        public string? SignatureData { get; set; }
        public DateTime ActedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
    }

    public class QualityAudit : BaseEntity
    {
        [Key]
        public int AuditId { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public AuditType AuditType { get; set; }

        [MaxLength(100)]
        public string? Standard { get; set; }

        [MaxLength(1000)]
        public string? Scope { get; set; }

        public int? DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public string LeadAuditorId { get; set; } = string.Empty;
        [ForeignKey(nameof(LeadAuditorId))]
        public ApplicationUser LeadAuditor { get; set; } = null!;

        public DateOnly PlannedStart { get; set; }
        public DateOnly PlannedEnd { get; set; }
        public DateOnly? ActualStart { get; set; }
        public DateOnly? ActualEnd { get; set; }

        public byte Status { get; set; } = 1;
        public string? Summary { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        [ForeignKey(nameof(CreatedBy))]
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<AuditFinding> Findings { get; set; } = [];
    }

    public class AuditFinding
    {
        [Key]
        public int FindingId { get; set; }
        public int AuditId { get; set; }
        [ForeignKey(nameof(AuditId))]
        public QualityAudit Audit { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public FindingType FindingType { get; set; }

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Evidence { get; set; }

        [MaxLength(100)]
        public string? ClauseRef { get; set; }

        public int? DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public string? ResponsibleId { get; set; }
        [ForeignKey(nameof(ResponsibleId))]
        public ApplicationUser? Responsible { get; set; }

        public DateOnly? DueDate { get; set; }
        public FindingStatus Status { get; set; } = FindingStatus.Abierta;

        [MaxLength(2000)]
        public string? CorrectiveAction { get; set; }

        public string? VerifiedById { get; set; }
        [ForeignKey(nameof(VerifiedById))]
        public ApplicationUser? VerifiedBy { get; set; }

        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public long LogId { get; set; }

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string EntityId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public string? ChangedBy { get; set; }
        [ForeignKey(nameof(ChangedBy))]
        public ApplicationUser? User { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public string? AdditionalInfo { get; set; }
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public byte NotificationType { get; set; } = 1;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}