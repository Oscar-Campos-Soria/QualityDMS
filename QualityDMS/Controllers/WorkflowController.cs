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
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkflowController(
        ApplicationDbContext context,
        IWorkflowService workflowService,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        IFileStorageService fileStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _workflowService = workflowService;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
    }

    // GET: /Workflow/Pending (renombrada desde MyPendingApprovals)
    public async Task<IActionResult> Pending()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        var pending = await _workflowService.GetPendingApprovalsAsync(userId, roles);
        return View(pending);
    }

    // GET: /Workflow/Approve/{instanceId}
    public async Task<IActionResult> Approve(int instanceId)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.Version)
                .ThenInclude(v => v.Document)
            .Include(i => i.Template)
                .ThenInclude(t => t.Steps)
            .FirstOrDefaultAsync(i => i.InstanceId == instanceId);

        if (instance == null)
            return NotFound();

        var currentStep = instance.Template?.Steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);
        if (currentStep == null)
            return NotFound("No se encontró el paso actual del flujo");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentStep.AssigneeId != userId)
            return Forbid();

        var model = new ApprovalActionViewModel
        {
            InstanceId = instance.InstanceId,
            VersionId = instance.VersionId,
            DocumentCode = instance.Version.Document.Code,
            DocumentTitle = instance.Version.Document.Title,
            VersionNumber = instance.Version.VersionNumber,
            StepName = currentStep.StepName,
            FilePath = instance.Version.FilePath
        };

        return View(model);
    }

    // POST: /Workflow/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApprovalActionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var result = await _workflowService.ProcessActionAsync(
            model.VersionId,
            userId,
            model.ActionType,
            model.Comments,
            model.SignatureData);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            // Registrar en auditoría
            await _auditLogService.LogAsync("Workflow", model.InstanceId.ToString(), "Approve",
                null, $"ActionType: {model.ActionType}", userId);
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Pending));
    }

    // GET: /Workflow/History/{documentId}
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

    // GET: /Workflow/DownloadVersion/{versionId}
    // (opcional, para que el aprobador descargue el archivo sin salir del flujo)
    public async Task<IActionResult> DownloadVersion(int versionId)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.VersionId == versionId);

        if (version == null)
            return NotFound();

        // Verificar que el usuario actual sea el asignado de algún paso activo
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isPending = await _context.WorkflowInstances
            .AnyAsync(i => i.VersionId == versionId &&
                           i.Status == WorkflowInstanceStatus.EnCurso &&
                           i.Template.Steps.Any(s => s.StepOrder == i.CurrentStep && s.AssigneeId == userId));

        if (!isPending && !User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
            return Forbid();

        var fileBytes = await _fileStorageService.GetFileAsync(version.FilePath);
        if (fileBytes == null)
            return NotFound("El archivo no se encuentra en el servidor.");

        return File(fileBytes, version.ContentType, version.OriginalFileName);
    }
}