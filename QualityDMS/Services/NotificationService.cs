using QualityDMS.Data;
using QualityDMS.Models;

namespace QualityDMS.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null);
    Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate)
    {
        // Obtener el paso actual y su asignado
        var instance = await _context.WorkflowInstances
            .Include(i => i.Template)
                .ThenInclude(t => t.Steps)
            .Include(i => i.Version)
                .ThenInclude(v => v.Document)
            .FirstOrDefaultAsync(i => i.InstanceId == workflowInstanceId);

        if (instance == null) return;

        var currentStep = instance.Template?.Steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);
        if (currentStep == null || string.IsNullOrEmpty(currentStep.AssigneeId)) return;

        var title = $"Nueva tarea: {instance.Version.Document.Code} - {stepName}";
        var message = $"Debe revisar el documento '{instance.Version.Document.Title}' versión {instance.Version.VersionNumber}. Fecha límite: {dueDate.ToLocalTime():dd/MM/yyyy}.";

        await NotifyUserAsync(currentStep.AssigneeId, title, message, "WorkflowInstance", workflowInstanceId.ToString());
    }
}