using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;
using System.Security.Cryptography;
using System.Text.Json;

namespace QualityDMS.Services;

// =========================================================================
// Interfaces
// =========================================================================

public interface IWorkflowService
{
    Task<WorkflowInstance> StartWorkflowAsync(int versionId, int templateId, string initiatorId);
    Task<(bool Success, string Message)> ProcessActionAsync(
        int versionId, string actorId, WorkflowActionType actionType,
        string? comments = null, string? signatureData = null);
    Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(string userId, IList<string> roles);
}

public interface IFileStorageService
{
    Task<(string FileName, string FilePath, string Hash)> SaveFileAsync(IFormFile file, string subfolder);
    Task<byte[]?> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
}

public interface IAuditLogService
{
    Task LogAsync(string entityType, string entityId, string action,
        string? oldValues, string? newValues, string? userId,
        string? ipAddress = null, string? additionalInfo = null);
    Task<List<AuditLog>> GetLogsAsync(string entityType, string entityId, int limit = 50);
}

public interface INotificationService
{
    // Métodos originales
    Task SendAsync(string userId, string title, string message, byte type = 1,
        string? entityType = null, string? entityId = null);
    Task NotifyDocumentStatusChangeAsync(int versionId, WorkflowActionType action, string actorId);

    // Nuevos métodos (integrados desde tu versión adicional)
    Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null);
    Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate);
}

// =========================================================================
// WorkflowService
// =========================================================================

public class WorkflowService : IWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        ApplicationDbContext db,
        INotificationService notifications,
        ILogger<WorkflowService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(int versionId, int templateId, string initiatorId)
    {
        var template = await _db.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(t => t.TemplateId == templateId)
            ?? throw new InvalidOperationException($"Plantilla {templateId} no encontrada.");

        var instance = new WorkflowInstance
        {
            VersionId = versionId,
            TemplateId = templateId,
            Status = WorkflowInstanceStatus.EnCurso,
            CurrentStep = 1,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatorId
        };

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync();

        var firstStep = template.Steps.FirstOrDefault(s => s.StepOrder == 1);
        if (firstStep?.AssigneeId is not null)
        {
            var version = await _db.DocumentVersions
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.VersionId == versionId);

            await _notifications.SendAsync(
                firstStep.AssigneeId,
                "Revisión/Aprobación Pendiente",
                $"El documento {version?.Document.Code} - {version?.Document.Title} " +
                $"requiere tu acción en el paso: {firstStep.StepName}",
                type: 4,
                entityType: "WorkflowInstance",
                entityId: instance.InstanceId.ToString());
        }

        return instance;
    }

    public async Task<(bool Success, string Message)> ProcessActionAsync(
        int versionId, string actorId, WorkflowActionType actionType,
        string? comments = null, string? signatureData = null)
    {
        var instance = await _db.WorkflowInstances
            .Include(w => w.Template)
                .ThenInclude(t => t!.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(w => w.VersionId == versionId && w.Status == WorkflowInstanceStatus.EnCurso);

        if (instance is null)
            return (false, "No existe un flujo activo para esta versión.");

        var steps = instance.Template!.Steps.OrderBy(s => s.StepOrder).ToList();
        var totalSteps = steps.Count;
        var currentStep = steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);

        if (currentStep is null)
            return (false, "Paso de flujo no encontrado.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.WorkflowActions.Add(new WorkflowAction
            {
                InstanceId = instance.InstanceId,
                StepOrder = currentStep.StepOrder,
                StepName = currentStep.StepName,
                StepType = currentStep.StepType,
                ActionType = actionType,
                ActorId = actorId,
                Comments = comments,
                SignatureData = signatureData,
                ActedAt = DateTime.UtcNow
            });

            var version = await _db.DocumentVersions
                .Include(v => v.Document)
                .FirstAsync(v => v.VersionId == versionId);

            if (actionType == WorkflowActionType.Rechazado ||
                actionType == WorkflowActionType.SolicitoCambios)  // ← Corregido
            {
                instance.Status = WorkflowInstanceStatus.Rechazado;
                instance.CompletedAt = DateTime.UtcNow;
                version.Status = DocumentStatus.Borrador;
                version.Document.CurrentStatus = DocumentStatus.Borrador;
            }
            else if (actionType == WorkflowActionType.Aprobado)
            {
                if (instance.CurrentStep >= totalSteps)
                {
                    instance.Status = WorkflowInstanceStatus.Completado;
                    instance.CompletedAt = DateTime.UtcNow;

                    var prevApproved = await _db.DocumentVersions
                        .Where(v => v.DocumentId == version.DocumentId &&
                                    v.VersionId != versionId &&
                                    v.Status == DocumentStatus.Aprobado)
                        .ToListAsync();

                    foreach (var prev in prevApproved)
                    {
                        prev.Status = DocumentStatus.Obsoleto;
                        prev.ObsoletedAt = DateTime.UtcNow;
                    }

                    version.Status = DocumentStatus.Aprobado;
                    version.PublishedAt = DateTime.UtcNow;
                    version.ApprovedById = actorId;
                    version.ApprovedAt = DateTime.UtcNow;

                    version.Document.CurrentStatus = DocumentStatus.Aprobado;
                    version.Document.CurrentVersionId = versionId;
                    version.Document.CurrentVersion = version.VersionNumber;
                    version.Document.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    instance.CurrentStep++;
                    version.Document.CurrentStatus = DocumentStatus.EnAprobacion;

                    var nextStep = steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);
                    if (nextStep?.AssigneeId is not null)
                    {
                        await _notifications.SendAsync(
                            nextStep.AssigneeId,
                            "Aprobación Requerida",
                            $"Documento {version.Document.Code} listo para: {nextStep.StepName}",
                            type: 4,
                            entityType: "WorkflowInstance",
                            entityId: instance.InstanceId.ToString());
                    }
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, "Acción procesada correctamente.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error procesando acción de workflow para versión {VersionId}", versionId);
            return (false, "Error interno al procesar la acción.");
        }
    }

    public async Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(string userId, IList<string> roles)
    {
        var instances = await _db.WorkflowInstances
            .Include(w => w.Version)
                .ThenInclude(v => v.Document)
            .Include(w => w.Template)
                .ThenInclude(t => t!.Steps)
            .Include(w => w.InitiatedByUser)
            .Where(w => w.Status == WorkflowInstanceStatus.EnCurso)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<PendingApprovalDto>();

        foreach (var inst in instances)
        {
            var step = inst.Template?.Steps.FirstOrDefault(s => s.StepOrder == inst.CurrentStep);
            if (step is null) continue;

            bool isResponsible = step.AssigneeId == userId ||
                                 (step.RoleRequired is not null && roles.Contains(step.RoleRequired));

            if (!isResponsible) continue;

            result.Add(new PendingApprovalDto
            {
                InstanceId = inst.InstanceId,
                VersionId = inst.VersionId,
                DocumentId = inst.Version.DocumentId,
                DocumentCode = inst.Version.Document.Code,
                DocumentTitle = inst.Version.Document.Title,
                VersionNumber = inst.Version.VersionNumber,
                StepName = step.StepName,
                StepType = (byte)step.StepType,
                CurrentStep = inst.CurrentStep,
                StartedAt = inst.StartedAt,
                DueDate = inst.StartedAt.AddDays(step.DaysAllowed),
                InitiatedBy = inst.InitiatedByUser.FullName
            });
        }

        return result.OrderBy(p => p.DueDate).ToList();
    }
}

// =========================================================================
// FileStorageService
// =========================================================================

public class FileStorageService : IFileStorageService
{
    private readonly string _baseUploadPath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        _baseUploadPath = config["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _logger = logger;
        Directory.CreateDirectory(_baseUploadPath);
    }

    public async Task<(string FileName, string FilePath, string Hash)> SaveFileAsync(IFormFile file, string subfolder)
    {
        var subDir = Path.Combine(_baseUploadPath, subfolder, DateTime.UtcNow.ToString("yyyy/MM"));
        Directory.CreateDirectory(subDir);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(subDir, fileName);
        var relativePath = Path.GetRelativePath(_baseUploadPath, fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        stream.Position = 0;
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream);
        var hash = Convert.ToHexString(hashBytes).ToLower();

        _logger.LogInformation("Archivo guardado: {Path}, Hash: {Hash}", relativePath, hash);
        return (fileName, relativePath, hash);
    }

    public async Task<byte[]?> GetFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_baseUploadPath, filePath);
        if (!File.Exists(fullPath)) return null;
        return await File.ReadAllBytesAsync(fullPath);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_baseUploadPath, filePath);
        if (!File.Exists(fullPath)) return Task.FromResult(false);
        File.Delete(fullPath);
        return Task.FromResult(true);
    }
}

// =========================================================================
// AuditLogService
// =========================================================================

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContext;

    public AuditLogService(ApplicationDbContext db, IHttpContextAccessor httpContext)
    {
        _db = db;
        _httpContext = httpContext;
    }

    public async Task LogAsync(string entityType, string entityId, string action,
        string? oldValues, string? newValues, string? userId,
        string? ipAddress = null, string? additionalInfo = null)
    {
        var ctx = _httpContext.HttpContext;
        var log = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress ?? ctx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ctx?.Request.Headers.UserAgent.ToString()?[..Math.Min(500, ctx.Request.Headers.UserAgent.ToString().Length)],
            AdditionalInfo = additionalInfo
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(string entityType, string entityId, int limit = 50)
    {
        return await _db.AuditLogs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .Include(l => l.User)
            .OrderByDescending(l => l.ChangedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }
}

// =========================================================================
// NotificationService (UNIFICADA)
// =========================================================================

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Método original (SendAsync)
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

    // Método original (NotifyDocumentStatusChangeAsync)
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
            WorkflowActionType.SolicitoCambios => "requiere cambios ✏️",
            _ => "fue actualizado"
        };

        var ownerId = version.Document.OwnerId;
        if (ownerId == actorId) return;

        await SendAsync(
            ownerId,
            $"Documento {version.Document.Code} {actionText}",
            $"El documento '{version.Document.Title}' (v{version.VersionNumber}) {actionText}.",
            type: action == WorkflowActionType.Aprobado ? (byte)1 : (byte)2,
            entityType: "Document",
            entityId: version.Document.DocumentId.ToString());
    }

    // Nuevo método (NotifyUserAsync) - es un alias de SendAsync
    public async Task NotifyUserAsync(string userId, string title, string message, string? entityType = null, string? entityId = null)
    {
        await SendAsync(userId, title, message, type: 1, entityType, entityId);
    }

    // Nuevo método (NotifyApproversAsync)
    public async Task NotifyApproversAsync(int workflowInstanceId, string stepName, DateTime dueDate)
    {
        // Usando FirstOrDefaultAsync con Microsoft.EntityFrameworkCore (asegúrate de tener el using)
        var instance = await _db.WorkflowInstances
            .Include(i => i.Template!)
                .ThenInclude(t => t.Steps)
            .Include(i => i.Version)
                .ThenInclude(v => v.Document)
            .FirstOrDefaultAsync(i => i.InstanceId == workflowInstanceId);

        if (instance == null) return;

        var currentStep = instance.Template?.Steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);
        if (currentStep == null || string.IsNullOrEmpty(currentStep.AssigneeId)) return;

        var title = $"Nueva tarea: {instance.Version.Document.Code} - {stepName}";
        var message = $"Debe revisar el documento '{instance.Version.Document.Title}' versión {instance.Version.VersionNumber}. Fecha límite: {dueDate.ToLocalTime():dd/MM/yyyy HH:mm}.";

        await NotifyUserAsync(currentStep.AssigneeId, title, message, "WorkflowInstance", workflowInstanceId.ToString());
    }
}