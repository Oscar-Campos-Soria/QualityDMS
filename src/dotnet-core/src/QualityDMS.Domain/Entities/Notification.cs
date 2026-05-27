using QualityDMS.Domain.Common;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Domain.Entities;

public class Notification : AuditableEntity
{
    public int NotificationId { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RelatedDocumentId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
