using QualityDMS.Domain.Common;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Domain.Entities;

public class QualityAudit : AuditableEntity
{
    public int AuditId { get; set; }
    public string AuditCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DepartmentId { get; set; }
    public AuditStatus Status { get; set; } = AuditStatus.Planned;
    public DateTime PlannedDate { get; set; }
    public DateTime? ExecutedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? AuditorUserId { get; set; }
    public string? Summary { get; set; }

    public Department Department { get; set; } = null!;
    public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
}
