using CalidadSYS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;
using QualityDMS.Infrastructure.Persistence;

namespace CalidadSYS.Controllers;

[Authorize]
public class AuditsController(QualityDMSDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
    {
        ViewData["Title"] = "Auditorías de Calidad";
        var query = db.QualityAudits
            .Include(a => a.Department)
            .Include(a => a.Findings)
            .OrderByDescending(a => a.PlannedDate);

        var totalCount = await query.CountAsync();
        var audits = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Pagination = new CalidadSYS.ViewModels.PaginationViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        return View(audits);
    }

    public async Task<IActionResult> Details(int id)
    {
        var audit = await db.QualityAudits
            .Include(a => a.Department)
            .Include(a => a.Findings)
            .FirstOrDefaultAsync(a => a.AuditId == id);

        if (audit is null) return NotFound();
        ViewData["Title"] = $"Auditoría: {audit.AuditCode}";
        return View(audit);
    }

    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Nueva Auditoría";
        return View(new AuditViewModel
        {
            PlannedDate = DateTime.Today.AddDays(7),
            Departments = await GetDepartmentsSelectList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> Create(AuditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsSelectList();
            return View(vm);
        }

        db.QualityAudits.Add(new QualityAudit
        {
            AuditCode = vm.AuditCode.Trim().ToUpper(),
            Title = vm.Title.Trim(),
            Description = vm.Description,
            DepartmentId = vm.DepartmentId,
            Status = AuditStatus.Planned,
            PlannedDate = vm.PlannedDate
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Auditoría creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var audit = await db.QualityAudits.FindAsync(id);
        if (audit is null) return NotFound();

        ViewData["Title"] = $"Editar: {audit.AuditCode}";
        return View(new AuditViewModel
        {
            AuditId = audit.AuditId,
            AuditCode = audit.AuditCode,
            Title = audit.Title,
            Description = audit.Description,
            DepartmentId = audit.DepartmentId,
            Status = audit.Status,
            PlannedDate = audit.PlannedDate,
            ExecutedDate = audit.ExecutedDate,
            ClosedDate = audit.ClosedDate,
            Summary = audit.Summary,
            Departments = await GetDepartmentsSelectList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> Edit(int id, AuditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsSelectList();
            return View(vm);
        }

        var audit = await db.QualityAudits.FindAsync(id);
        if (audit is null) return NotFound();

        audit.AuditCode = vm.AuditCode.Trim().ToUpper();
        audit.Title = vm.Title.Trim();
        audit.Description = vm.Description;
        audit.DepartmentId = vm.DepartmentId;
        audit.Status = vm.Status;
        audit.PlannedDate = vm.PlannedDate;
        audit.ExecutedDate = vm.ExecutedDate;
        audit.ClosedDate = vm.ClosedDate;
        audit.Summary = vm.Summary;

        await db.SaveChangesAsync();
        TempData["Success"] = "Auditoría actualizada.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> AddFinding(AuditFindingViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Datos del hallazgo inválidos.";
            return RedirectToAction(nameof(Details), new { id = vm.AuditId });
        }

        db.AuditFindings.Add(new AuditFinding
        {
            AuditId = vm.AuditId,
            Description = vm.Description,
            FindingType = vm.FindingType,
            CorrectiveAction = vm.CorrectiveAction,
            DueDate = vm.DueDate
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Hallazgo agregado.";
        return RedirectToAction(nameof(Details), new { id = vm.AuditId });
    }

    private async Task<IEnumerable<SelectListItem>> GetDepartmentsSelectList() =>
        (await db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync())
        .Select(d => new SelectListItem(d.Name, d.DepartmentId.ToString()));
}
