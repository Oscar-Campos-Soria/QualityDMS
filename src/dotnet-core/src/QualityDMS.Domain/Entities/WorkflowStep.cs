using QualityDMS.Domain.Common;

namespace QualityDMS.Domain.Entities;

public class WorkflowStep : AuditableEntity
{
    public int WorkflowStepId { get; set; }
    public int WorkflowTemplateId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssignedRoleName { get; set; }
    public string? AssignedUserId { get; set; }
    public bool RequiresAllApprovers { get; set; } = false;

    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
}
