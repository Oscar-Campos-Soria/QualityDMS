using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;
using QualityDMS.Services;
using System.Security.Claims;

namespace QualityDMS.Controllers;

[Authorize]
public class WorkflowController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPostgreSyncService _postgresSync;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkflowController(
        ApplicationDbContext context,
        IWorkflowService workflowService,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        IFileStorageService fileStorageService,
        IPostgreSyncService postgresSync,
        UserManager<ApplicationUser> userManager)
    {
        _context             = context;
        _workflowService     = workflowService;
        _notificationService = notificationService;
        _auditLogService     = auditLogService;
        _fileStorageService  = fileStorageService;
        _postgresSync        = postgresSync;
        _userManager         = userManager;
    }

    // ── GET /Workflow/Pending ─────────────────────────────────
    public async Task<IActionResult> Pending()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var user  = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user!);

        var pending = await _workflowService.GetPendingApprovalsAsync(userId, roles);
        return View(pending);
    }

    // ── GET /Workflow/Approve/{instanceId} ────────────────────
    [Authorize(Roles = "Admin,QualityManager,Approver,Reviewer")]
    public async Task<IActionResult> Approve(int instanceId)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.Version)
                .ThenInclude(v => v.Document)
            .Include(i => i.Template)
                .ThenInclude(t => t!.Steps)
            .FirstOrDefaultAsync(i => i.InstanceId == instanceId);

        if (instance is null) return NotFound();

        var currentStep = instance.Template?.Steps
            .FirstOrDefault(s => s.StepOrder == instance.CurrentStep);

        if (currentStep is null)
            return NotFound("No se encontró el paso actual del flujo.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user   = await _userManager.FindByIdAsync(userId!);
        var roles  = await _userManager.GetRolesAsync(user!);

        // Verificar que el usuario puede actuar en este paso
        bool canAct = currentStep.AssigneeId == userId ||
                      (currentStep.RoleRequired is not null &&
                       roles.Contains(currentStep.RoleRequired));

        if (!canAct) return Forbid();

        var model = new ApprovalActionViewModel
        {
            InstanceId    = instance.InstanceId,
            VersionId     = instance.VersionId,
            DocumentCode  = instance.Version.Document.Code,
            DocumentTitle = instance.Version.Document.Title,
            VersionNumber = instance.Version.VersionNumber,
            StepName      = currentStep.StepName,
            FilePath      = instance.Version.FilePath
        };

        return View(model);
    }

    // ── POST /Workflow/Approve ────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,QualityManager,Approver,Reviewer")]
    public async Task<IActionResult> Approve(ApprovalActionViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Se crea la estrategia de ejecución para permitir transacciones manuales internas con reintentos habilitados
        var strategy = _context.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            return await _workflowService.ProcessActionAsync(
                model.VersionId,
                userId,
                model.ActionType,
                model.Comments,
                model.SignatureData);
        });

        if (result.Success)
        {
            // ── Sincronizar a PostgreSQL si el documento quedó Aprobado ──
            if (model.ActionType == WorkflowActionType.Aprobado)
            {
                var version = await _context.DocumentVersions
                    .Include(v => v.Document)
                        .ThenInclude(d => d.Category)
                    .Include(v => v.Document)
                        .ThenInclude(d => d.Department)
                    .FirstOrDefaultAsync(v => v.VersionId == model.VersionId);

                if (version is not null &&
                    version.Document.CurrentStatus == DocumentStatus.Aprobado)
                {
                    // Sync en background para no bloquear la respuesta al usuario
                    _ = Task.Run(() =>
                        _postgresSync.SyncApprovedDocumentAsync(version.Document, version));
                }
            }

            await _auditLogService.LogAsync(
                "Workflow", model.InstanceId.ToString(), "Approve",
                null, $"ActionType: {model.ActionType}", userId);

            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Pending));
    }

    // ── GET /Workflow/History/{documentId} ────────────────────
    public async Task<IActionResult> History(int documentId)
    {
        var instances = await _context.WorkflowInstances
            .Include(i => i.Template)
            .Include(i => i.Actions)
                .ThenInclude(a => a.Actor)
            .Include(i => i.Version)
            .Where(i => i.Version.DocumentId == documentId)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync();

        ViewBag.DocumentId = documentId;
        return View(instances);
    }

    // ── GET /Workflow/DownloadVersion/{versionId} ─────────────
    public async Task<IActionResult> DownloadVersion(int versionId)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.VersionId == versionId);

        if (version is null) return NotFound();

        var userId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isPending = await _context.WorkflowInstances
            .AnyAsync(i => i.VersionId == versionId &&
                           i.Status == WorkflowInstanceStatus.EnCurso &&
                           i.Template!.Steps.Any(s =>
                               s.StepOrder == i.CurrentStep &&
                               s.AssigneeId == userId));

        if (!isPending && !User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
            return Forbid();

        var fileBytes = await _fileStorageService.GetFileAsync(version.FilePath);
        if (fileBytes is null)
            return NotFound("El archivo no se encuentra en el servidor.");

        return File(fileBytes, version.ContentType, version.OriginalFileName);
    }







// ── GET /Workflow/ViewPdf/{versionId} ─────────────
    public async Task<IActionResult> ViewPdf(int versionId)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.VersionId == versionId);

        if (version is null) return NotFound();

        // Mismas validaciones de seguridad que el download
        var userId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isPending = await _context.WorkflowInstances
            .AnyAsync(i => i.VersionId == versionId &&
                           i.Status == WorkflowInstanceStatus.EnCurso &&
                           i.Template!.Steps.Any(s =>
                               s.StepOrder == i.CurrentStep &&
                               s.AssigneeId == userId));

        if (!isPending && !User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
            return Forbid();

        var fileBytes = await _fileStorageService.GetFileAsync(version.FilePath);
        if (fileBytes is null)
            return NotFound("El archivo no se encuentra en el servidor.");

        // Al NO pasar el originalFileName, el navegador intentará renderizarlo en el iframe
        return File(fileBytes, "application/pdf");
    }



}