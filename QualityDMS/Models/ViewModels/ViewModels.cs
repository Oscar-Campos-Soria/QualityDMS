using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace QualityDMS.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public int Borradores { get; set; }
        public int EnRevision { get; set; }
        public int EnAprobacion { get; set; }
        public int Aprobados { get; set; }
        public int Obsoletos { get; set; }
        public int PorVencer { get; set; }

        public List<CategoryCountItem> DocumentsByCategory { get; set; } = [];
        public List<DocumentSummaryDto> UpcomingReviews { get; set; } = [];
        public List<PendingApprovalDto> PendingApprovals { get; set; } = [];
        public List<Notification> RecentNotifications { get; set; } = [];
        public int UnreadNotifications { get; set; }
    }

    public record CategoryCountItem(string Category, int Total);

    public class DocumentIndexViewModel
    {
        public List<DocumentSummaryDto> Documents { get; set; } = [];
        public DocumentFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class DocumentFilterViewModel
    {
        public DocumentStatus? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public SelectList? Categories { get; set; }
        public SelectList? Departments { get; set; }
        public SelectList? Statuses { get; set; }
    }

    public class DocumentSummaryDto
    {
        public int DocumentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public byte CurrentStatus { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusBadge { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public bool IsConfidential { get; set; }
        public DateOnly? NextReviewDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string FileSizeFormatted { get; set; } = string.Empty;
    }

    public class DocumentCreateViewModel
    {
        [Required(ErrorMessage = "El código es obligatorio")]
        [MaxLength(50), Display(Name = "Código del Documento")]
        [RegularExpression(@"^[A-Z]{2,5}-[A-Z]{2,5}-\d{3,5}$",
            ErrorMessage = "Formato: CAT-DEP-001")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(300), Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000), Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Required, Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        [Required, Display(Name = "Departamento")]
        public int DepartmentId { get; set; }

        [Display(Name = "Etiquetas (separadas por coma")]
        public string? Tags { get; set; }

        [Display(Name = "Documento Confidencial")]
        public bool IsConfidential { get; set; } = false;

        [Display(Name = "Próxima Fecha de Revisión")]
        [DataType(DataType.Date)]
        public DateOnly? NextReviewDate { get; set; }

        [Required(ErrorMessage = "Debe adjuntar el archivo del documento")]
        [Display(Name = "Archivo del Documento")]
        public IFormFile? File { get; set; }

        [MaxLength(2000), Display(Name = "Descripción del Cambio / Nota de Versión")]
        public string? ChangeLog { get; set; }

        [Required, Display(Name = "Flujo de Aprobación")]
        public int WorkflowTemplateId { get; set; }

        public SelectList? Categories { get; set; }
        public SelectList? Departments { get; set; }
        public SelectList? WorkflowTemplates { get; set; }
    }

    public class DocumentEditViewModel
    {
        public int DocumentId { get; set; }

        [Required, MaxLength(300), Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000), Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Required, Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        [Required, Display(Name = "Departamento")]
        public int DepartmentId { get; set; }

        [Display(Name = "Etiquetas")]
        public string? Tags { get; set; }

        [Display(Name = "Confidencial")]
        public bool IsConfidential { get; set; }

        [Display(Name = "Próxima Revisión")]
        [DataType(DataType.Date)]
        public DateOnly? NextReviewDate { get; set; }

        public SelectList? Categories { get; set; }
        public SelectList? Departments { get; set; }
    }

    public class DocumentDetailViewModel
    {
        public Document Document { get; set; } = null!;
        public List<DocumentVersion> Versions { get; set; } = [];
        public WorkflowInstance? ActiveWorkflow { get; set; }
        public List<WorkflowAction> WorkflowHistory { get; set; } = [];
        public bool CanEdit { get; set; }
        public bool CanUploadVersion { get; set; }
        public bool CanApprove { get; set; }
        public bool CanObsolete { get; set; }
        public PendingApprovalDto? PendingAction { get; set; }
    }

    public class NewVersionViewModel
    {
        public int DocumentId { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe adjuntar el archivo")]
        [Display(Name = "Archivo")]
        public IFormFile? File { get; set; }

        [Required, Display(Name = "Tipo de Versión")]
        public VersionType VersionType { get; set; } = VersionType.Menor;

        [Required, MaxLength(2000), Display(Name = "Descripción de Cambios")]
        public string ChangeLog { get; set; } = string.Empty;

        [Required, Display(Name = "Flujo de Aprobación")]
        public int WorkflowTemplateId { get; set; }

        [Display(Name = "Fecha Efectiva")]
        [DataType(DataType.Date)]
        public DateOnly? EffectiveDate { get; set; }

        public SelectList? WorkflowTemplates { get; set; }

        public string ProposedVersion { get; set; } = string.Empty;
    }

    public class PendingApprovalDto
    {
        public int InstanceId { get; set; }
        public int VersionId { get; set; }
        public int DocumentId { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string VersionNumber { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public byte StepType { get; set; }
        public int CurrentStep { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime DueDate { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public bool IsOverdue => DueDate < DateTime.UtcNow;
    }

    public class ApprovalActionViewModel
    {
        public int InstanceId { get; set; }
        public int VersionId { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string VersionNumber { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        [Required, Display(Name = "Acción")]
        public WorkflowActionType ActionType { get; set; }

        [MaxLength(2000), Display(Name = "Comentarios")]
        public string? Comments { get; set; }

        [Display(Name = "Firma Digital (Base64)")]
        public string? SignatureData { get; set; }
    }

    public class AuditCreateViewModel
    {
        [Required, MaxLength(300), Display(Name = "Título de la Auditoría")]
        public string Title { get; set; } = string.Empty;

        [Required, Display(Name = "Tipo de Auditoría")]
        public AuditType AuditType { get; set; }

        [MaxLength(100), Display(Name = "Norma / Estándar")]
        public string? Standard { get; set; }

        [MaxLength(1000), Display(Name = "Alcance")]
        public string? Scope { get; set; }

        public int? DepartmentId { get; set; }

        [Required, Display(Name = "Auditor Líder")]
        public string LeadAuditorId { get; set; } = string.Empty;

        [Required, DataType(DataType.Date), Display(Name = "Inicio Planeado")]
        public DateOnly PlannedStart { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required, DataType(DataType.Date), Display(Name = "Fin Planeado")]
        public DateOnly PlannedEnd { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        public SelectList? Departments { get; set; }
        public SelectList? Auditors { get; set; }
    }

    public class FindingCreateViewModel
    {
        public int AuditId { get; set; }
        public string AuditCode { get; set; } = string.Empty;

        [Required, Display(Name = "Tipo de Hallazgo")]
        public FindingType FindingType { get; set; }

        [Required, MaxLength(2000), Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000), Display(Name = "Evidencia")]
        public string? Evidence { get; set; }

        [MaxLength(100), Display(Name = "Cláusula de la Norma")]
        public string? ClauseRef { get; set; }

        public int? DepartmentId { get; set; }

        [Display(Name = "Responsable")]
        public string? ResponsibleId { get; set; }

        [DataType(DataType.Date), Display(Name = "Fecha Límite")]
        public DateOnly? DueDate { get; set; }

        [MaxLength(2000), Display(Name = "Acción Correctiva")]
        public string? CorrectiveAction { get; set; }

        public SelectList? Departments { get; set; }
        public SelectList? Users { get; set; }
    }

    public class GlobalSearchViewModel
    {
        public string? Query { get; set; }
        public List<DocumentSummaryDto> Documents { get; set; } = [];
        public List<QualityAudit> Audits { get; set; } = [];
        public int TotalResults => Documents.Count + Audits.Count;
    }
}