using QualityDMS.Domain.Common;

namespace QualityDMS.Domain.Entities;

public class ControlledDistribution : AuditableEntity
{
    public int DistributionId { get; set; }
    public int DocumentId { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsAcknowledged { get; set; }

    public Document Document { get; set; } = null!;
}
