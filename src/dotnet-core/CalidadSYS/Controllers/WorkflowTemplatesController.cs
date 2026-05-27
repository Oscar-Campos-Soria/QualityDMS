using CalidadSYS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Infrastructure.Identity;
using QualityDMS.Infrastructure.Persistence;

namespace CalidadSYS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class WorkflowTemplatesController(
    QualityDMSDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    private static readonly string[] AvailableRoles =
        ["Admin", "QualityManager", "DocumentManager", "Approver"];

    public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
    {
        ViewData["Title"] = "Plantillas de Workflow";
        var query = db.WorkflowTemplates
            .Include(t => t.Steps)
            .OrderBy(t => t.Name);

        var totalCount = await query.CountAsync();
        var templates = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Pagination = new CalidadSYS.ViewModels.PaginationViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        return View(templates);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Nueva Plantilla";
        return View(new WorkflowTemplateViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkflowTemplateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (await db.WorkflowTemplates.AnyAsync(t => t.Name == vm.Name.Trim()))
        {
            ModelState.AddModelError(nameof(vm.Name), "Ya existe una plantilla con ese nombre.");
            return View(vm);
        }

        var template = new WorkflowTemplate
        {
            Name = vm.Name.Trim(),
            Description = vm.Description?.Trim(),
            IsActive = vm.IsActive,
            CreatedBy = User.Identity!.Name!
        };

        db.WorkflowTemplates.Add(template);
        await db.SaveChangesAsync();

        TempData["Success"] = "Plantilla creada. Ahora agrega los pasos del flujo.";
        return RedirectToAction(nameof(Steps), new { id = template.WorkflowTemplateId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var template = await db.WorkflowTemplates.FindAsync(id);
        if (template is null) return NotFound();

        ViewData["Title"] = $"Editar: {template.Name}";
        return View(new WorkflowTemplateViewModel
        {
            WorkflowTemplateId = template.WorkflowTemplateId,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkflowTemplateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var template = await db.WorkflowTemplates.FindAsync(id);
        if (template is null) return NotFound();

        if (await db.WorkflowTemplates.AnyAsync(t => t.Name == vm.Name.Trim() && t.WorkflowTemplateId != id))
        {
            ModelState.AddModelError(nameof(vm.Name), "Ya existe otra plantilla con ese nombre.");
            return View(vm);
        }

        template.Name = vm.Name.Trim();
        template.Description = vm.Description?.Trim();
        template.IsActive = vm.IsActive;
        template.UpdatedBy = User.Identity!.Name!;

        await db.SaveChangesAsync();
        TempData["Success"] = "Plantilla actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var template = await db.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == id);
        if (template is null) return NotFound();

        ViewData["Title"] = $"Eliminar: {template.Name}";
        return View(template);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (await db.Documents.AnyAsync(d => d.WorkflowTemplateId == id))
        {
            TempData["Error"] = "No se puede eliminar: hay documentos que usan esta plantilla. Desactívela en su lugar.";
            return RedirectToAction(nameof(Index));
        }

        var template = await db.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == id);
        if (template is null) return NotFound();

        db.WorkflowTemplates.Remove(template);
        await db.SaveChangesAsync();
        TempData["Success"] = "Plantilla eliminada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Steps(int id)
    {
        var template = await db.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == id);
        if (template is null) return NotFound();

        ViewData["Title"] = $"Pasos: {template.Name}";

        var users = await userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync();

        var nextOrder = template.Steps.Any() ? template.Steps.Max(s => s.StepOrder) + 1 : 1;

        var vm = new WorkflowTemplateStepsViewModel
        {
            WorkflowTemplateId = template.WorkflowTemplateId,
            TemplateName = template.Name,
            TemplateDescription = template.Description,
            IsActive = template.IsActive,
            Steps = template.Steps.Select(s => new WorkflowStepRowViewModel
            {
                WorkflowStepId = s.WorkflowStepId,
                StepOrder = s.StepOrder,
                StepName = s.StepName,
                Description = s.Description,
                AssignedDisplay = s.AssignedUserId is not null
                    ? users.FirstOrDefault(u => u.Id == s.AssignedUserId)?.FullName ?? s.AssignedUserId
                    : s.AssignedRoleName is not null ? $"Rol: {s.AssignedRoleName}" : "Sin asignar",
                RequiresAllApprovers = s.RequiresAllApprovers
            }).ToList(),
            NewStep = new WorkflowStepViewModel
            {
                WorkflowTemplateId = id,
                StepOrder = nextOrder,
                AssignmentType = "role",
                Users = BuildUsersSelectList(users),
                Roles = BuildRolesSelectList()
            }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStep()
    {
        var vm = new WorkflowStepViewModel();
        await TryUpdateModelAsync(vm, "NewStep");

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.StepName))
        {
            TempData["Error"] = "Datos del paso inválidos. Verifica los campos requeridos.";
            return RedirectToAction(nameof(Steps), new { id = vm.WorkflowTemplateId });
        }

        if (await db.WorkflowSteps.AnyAsync(s =>
            s.WorkflowTemplateId == vm.WorkflowTemplateId && s.StepOrder == vm.StepOrder))
        {
            TempData["Error"] = $"Ya existe un paso con orden {vm.StepOrder} en esta plantilla.";
            return RedirectToAction(nameof(Steps), new { id = vm.WorkflowTemplateId });
        }

        var step = new WorkflowStep
        {
            WorkflowTemplateId = vm.WorkflowTemplateId,
            StepName = vm.StepName.Trim(),
            Description = vm.Description?.Trim(),
            StepOrder = vm.StepOrder,
            AssignedUserId = vm.AssignmentType == "user" ? vm.AssignedUserId : null,
            AssignedRoleName = vm.AssignmentType == "role" ? vm.AssignedRoleName : null,
            RequiresAllApprovers = vm.RequiresAllApprovers,
            CreatedBy = User.Identity!.Name!
        };

        db.WorkflowSteps.Add(step);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Paso '{step.StepName}' agregado.";
        return RedirectToAction(nameof(Steps), new { id = vm.WorkflowTemplateId });
    }

    public async Task<IActionResult> EditStep(int stepId)
    {
        var step = await db.WorkflowSteps.FindAsync(stepId);
        if (step is null) return NotFound();

        var template = await db.WorkflowTemplates.FindAsync(step.WorkflowTemplateId);
        ViewData["Title"] = $"Editar paso - {template?.Name}";

        var users = await userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync();

        return View(new WorkflowStepViewModel
        {
            WorkflowStepId = step.WorkflowStepId,
            WorkflowTemplateId = step.WorkflowTemplateId,
            StepName = step.StepName,
            Description = step.Description,
            StepOrder = step.StepOrder,
            AssignmentType = step.AssignedUserId is not null ? "user" : "role",
            AssignedUserId = step.AssignedUserId,
            AssignedRoleName = step.AssignedRoleName,
            RequiresAllApprovers = step.RequiresAllApprovers,
            Users = BuildUsersSelectList(users),
            Roles = BuildRolesSelectList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStep(int stepId, WorkflowStepViewModel vm)
    {
        var step = await db.WorkflowSteps.FindAsync(stepId);
        if (step is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var users = await userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .ToListAsync();
            vm.Users = BuildUsersSelectList(users);
            vm.Roles = BuildRolesSelectList();
            return View(vm);
        }

        if (await db.WorkflowSteps.AnyAsync(s =>
            s.WorkflowTemplateId == step.WorkflowTemplateId &&
            s.StepOrder == vm.StepOrder &&
            s.WorkflowStepId != stepId))
        {
            ModelState.AddModelError(nameof(vm.StepOrder), $"Ya existe otro paso con orden {vm.StepOrder}.");
            var users2 = await userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .ToListAsync();
            vm.Users = BuildUsersSelectList(users2);
            vm.Roles = BuildRolesSelectList();
            return View(vm);
        }

        step.StepName = vm.StepName.Trim();
        step.Description = vm.Description?.Trim();
        step.StepOrder = vm.StepOrder;
        step.AssignedUserId = vm.AssignmentType == "user" ? vm.AssignedUserId : null;
        step.AssignedRoleName = vm.AssignmentType == "role" ? vm.AssignedRoleName : null;
        step.RequiresAllApprovers = vm.RequiresAllApprovers;
        step.UpdatedBy = User.Identity!.Name!;

        await db.SaveChangesAsync();
        TempData["Success"] = "Paso actualizado.";
        return RedirectToAction(nameof(Steps), new { id = step.WorkflowTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStep(int stepId)
    {
        var step = await db.WorkflowSteps.FindAsync(stepId);
        if (step is null) return NotFound();

        var templateId = step.WorkflowTemplateId;
        db.WorkflowSteps.Remove(step);
        await db.SaveChangesAsync();

        TempData["Success"] = "Paso eliminado.";
        return RedirectToAction(nameof(Steps), new { id = templateId });
    }

    private static IEnumerable<SelectListItem> BuildUsersSelectList(List<ApplicationUser> users) =>
        users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id));

    private static IEnumerable<SelectListItem> BuildRolesSelectList() =>
        AvailableRoles.Select(r => new SelectListItem(r, r));
}
