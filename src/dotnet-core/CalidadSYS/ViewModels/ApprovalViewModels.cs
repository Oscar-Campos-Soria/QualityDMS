using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class ReviewApprovalViewModel
{
    public int DocumentId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    [Display(Name = "Comentarios")]
    [StringLength(1000)]
    public string? Comments { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    [Display(Name = "Motivo de rechazo")]
    [StringLength(500)]
    public string? RejectReason { get; set; }
}
