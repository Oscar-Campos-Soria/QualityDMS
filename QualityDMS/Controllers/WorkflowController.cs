using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;

namespace QualityDMS.Controllers;

[Authorize]
public class WorkflowController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public WorkflowController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /Workflow/MyPendingApprovals
    public async Task<IActionResult> MyPendingApprovals()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        // Buscar instancias de flujo donde el usuario sea el asignado del paso actual
        var pending = await (from wi in _context.WorkflowInstances
                             join wts in _context.WorkflowTemplateSteps
                                 on wi.TemplateId equals wts.TemplateId
                             where wi.Status == WorkflowInstanceStatus.EnCurso
                                 && wi.CurrentStep == wts.StepOrder
                                 && wts.AssigneeId == userId
                             join dv in _context.DocumentVersions on wi.VersionId equals dv.VersionId
                             join d in _context.Documents on dv.DocumentId equals d.DocumentId
                             join u in _context.Users on wi.InitiatedBy equals u.Id
                             select new PendingApprovalDto
                             {
                                 InstanceId = wi.InstanceId,
                                 VersionId = dv.VersionId,
                                 DocumentId = d.DocumentId,
                                 DocumentCode = d.Code,
                                 DocumentTitle = d.Title,
                                 VersionNumber = dv.VersionNumber,
                                 StepName = wts.StepName,
                                 StepType = (byte)wts.StepType,
                                 CurrentStep = wi.CurrentStep,
                                 StartedAt = wi.StartedAt,
                                 DueDate = wi.StartedAt.AddDays(wts.DaysAllowed),
                                 InitiatedBy = u.FullName ?? u.UserName ?? ""
                             })
                             .ToListAsync();

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

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

        var instance = await _context.WorkflowInstances
            .Include(i => i.Version)
            .Include(i => i.Template)
                .ThenInclude(t => t.Steps)
            .FirstOrDefaultAsync(i => i.InstanceId == model.InstanceId);

        if (instance == null)
            return NotFound();

        var currentStep = instance.Template?.Steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStep);
        if (currentStep == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentStep.AssigneeId != userId)
            return Forbid();

        // Registrar la acción
        var action = new WorkflowAction
        {
            InstanceId = instance.InstanceId,
            StepOrder = instance.CurrentStep,
            StepName = currentStep.StepName,
            StepType = currentStep.StepType,
            ActionType = model.ActionType,
            ActorId = userId!,
            Comments = model.Comments,
            SignatureData = model.SignatureData,
            ActedAt = DateTime.UtcNow
        };
        _context.WorkflowActions.Add(action);

        // Si la acción es rechazar o solicitar cambios
        if (model.ActionType == WorkflowActionType.Rechazado || model.ActionType == WorkflowActionType.SolicitoSambios)
        {
            instance.Status = WorkflowInstanceStatus.Rechazado;
            instance.CompletedAt = DateTime.UtcNow;

            // Cambiar estado del documento a Borrador (para que pueda corregirse)
            var doc = await _context.Documents.FindAsync(instance.Version.DocumentId);
            if (doc != null)
                doc.CurrentStatus = DocumentStatus.Borrador;

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Documento rechazado. Se notificará al autor.";
            return RedirectToAction(nameof(MyPendingApprovals));
        }

        // Si es aprobado, avanzar al siguiente paso
        var nextStepOrder = instance.CurrentStep + 1;
        var nextStep = instance.Template?.Steps.FirstOrDefault(s => s.StepOrder == nextStepOrder);

        if (nextStep == null)
        {
            // No hay más pasos: flujo completado
            instance.Status = WorkflowInstanceStatus.Completado;
            instance.CompletedAt = DateTime.UtcNow;

            // Actualizar estado del documento y versión a Aprobado
            var doc = await _context.Documents.FindAsync(instance.Version.DocumentId);
            if (doc != null)
            {
                doc.CurrentStatus = DocumentStatus.Aprobado;
                doc.CurrentVersionId = instance.VersionId;
                doc.CurrentVersion = instance.Version.VersionNumber;
                doc.UpdatedAt = DateTime.UtcNow;
            }

            var version = await _context.DocumentVersions.FindAsync(instance.VersionId);
            if (version != null)
            {
                version.Status = DocumentStatus.Aprobado;
                version.PublishedAt = DateTime.UtcNow;
                version.ApprovedById = userId;
                version.ApprovedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Documento aprobado y publicado correctamente.";
            return RedirectToAction(nameof(MyPendingApprovals));
        }

        // Avanzar al siguiente paso
        instance.CurrentStep = nextStepOrder;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = $"Aprobado. Siguiente paso: {nextStep.StepName}.";
        return RedirectToAction(nameof(MyPendingApprovals));
    }
}