using QualityDMS.Domain.Common;

namespace QualityDMS.Domain.Entities;

public class AuditFinding : AuditableEntity
{
    public int FindingId { get; set; }
    public int AuditId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FindingType { get; set; } = string.Empty; // NC, Observation, Improvement
    public string? CorrectiveAction { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedDate { get; set; }

    public QualityAudit Audit { get; set; } = null!;
}
