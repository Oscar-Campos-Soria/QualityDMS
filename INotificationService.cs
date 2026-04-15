// ── NotificationService ──────────────────────────────────────

public interface INotificationService
{
    Task SendAsync(string userId, string title, string message, byte type = 1,
        string? entityType = null, string? entityId = null);
    Task NotifyDocumentStatusChangeAsync(int versionId, WorkflowActionType action, string actorId);
    Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null);
    Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task SendAsync(string userId, string title, string message, byte type = 1,
        string? entityType = null, string? entityId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = type,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task NotifyDocumentStatusChangeAsync(int versionId, WorkflowActionType action, string actorId)
    {
        var version = await _db.DocumentVersions
            .Include(v => v.Document)
                .ThenInclude(d => d.Owner)
            .FirstOrDefaultAsync(v => v.VersionId == versionId);

        if (version is null) return;

        var actionText = action switch
        {
            WorkflowActionType.Aprobado => "fue aprobado ✅",
            WorkflowActionType.Rechazado => "fue rechazado ❌",
            WorkflowActionType.SolicitoSambios => "requiere cambios ✏️",
            _ => "fue actualizado"
        };

        var ownerId = version.Document.OwnerId;
        if (ownerId == actorId) return; // No notificar al mismo actor

        await SendAsync(
            ownerId,
            $"Documento {version.Document.Code} {actionText}",
            $"El documento '{version.Document.Title}' (v{version.VersionNumber}) {actionText}.",
            type: action == WorkflowActionType.Aprobado ? (byte)1 : (byte)2,
            entityType: "Document",
            entityId: version.Document.DocumentId.ToString());
    }

    // Reutiliza SendAsync para notificaciones genéricas a un usuario
    public async Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null)
    {
        await SendAsync(userId, title, message, type: 1, entityType: entityType, entityId: entityId);
    }

    // Lógica de notificación a los aprobadores (adaptada desde NotificationService.cs)
    public async Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate)
    {
        var instance = await _db.WorkflowInstances
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