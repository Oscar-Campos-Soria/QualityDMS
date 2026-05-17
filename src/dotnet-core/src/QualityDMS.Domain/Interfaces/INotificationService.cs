using QualityDMS.Domain.Enums;

namespace QualityDMS.Domain.Interfaces;

public interface INotificationService
{
    Task SendAsync(string recipientUserId, NotificationType type, string title, string message,
        int? relatedDocumentId = null, CancellationToken ct = default);
    Task SendToRoleAsync(string roleName, NotificationType type, string title, string message,
        int? relatedDocumentId = null, CancellationToken ct = default);
}
