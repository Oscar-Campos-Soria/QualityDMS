using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;
using QualityDMS.Domain.Interfaces;
using QualityDMS.Infrastructure.Identity;
using QualityDMS.Infrastructure.Persistence;

namespace QualityDMS.Infrastructure.Services;

public class NotificationService(QualityDMSDbContext ctx, UserManager<ApplicationUser> userManager)
    : INotificationService
{
    public async Task SendAsync(string recipientUserId, NotificationType type, string title,
        string message, int? relatedDocumentId = null, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            RelatedDocumentId = relatedDocumentId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        await ctx.Notifications.AddAsync(notification, ct);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task SendToRoleAsync(string roleName, NotificationType type, string title,
        string message, int? relatedDocumentId = null, CancellationToken ct = default)
    {
        var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
        foreach (var user in usersInRole.Where(u => u.IsActive))
        {
            await SendAsync(user.Id, type, title, message, relatedDocumentId, ct);
        }
    }
}
