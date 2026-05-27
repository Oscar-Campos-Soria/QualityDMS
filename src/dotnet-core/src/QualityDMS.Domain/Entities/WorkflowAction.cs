using QualityDMS.Domain.Common;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Domain.Entities;

public class WorkflowAction : AuditableEntity
{
    public int WorkflowActionId { get; set; }
    public int WorkflowInstanceId { get; set; }
    public int StepOrder { get; set; }
    public string ActionByUserId { get; set; } = string.Empty;
    public WorkflowStepStatus Action { get; set; }
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; }

    public WorkflowInstance WorkflowInstance { get; set; } = null!;
}
