using CalidadSYS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Infrastructure.Persistence;

namespace CalidadSYS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class DepartmentsController(QualityDMSDbContext db) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
    {
        ViewData["Title"] = "Departamentos";
        var query = db.Departments.OrderBy(d => d.Name);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Pagination = new CalidadSYS.ViewModels.PaginationViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var dept = await db.Departments
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.DepartmentId == id);

        if (dept is null) return NotFound();
        ViewData["Title"] = $"Departamento: {dept.Name}";
        return View(dept);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo Departamento";
        return View(new DepartmentViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (await db.Departments.AnyAsync(d => d.Code == vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "Ya existe un departamento con ese código.");
            return View(vm);
        }

        db.Departments.Add(new Department
        {
            Code = vm.Code.Trim().ToUpper(),
            Name = vm.Name.Trim(),
            Description = vm.Description,
            ManagerName = vm.ManagerName,
            IsActive = vm.IsActive
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Departamento creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        ViewData["Title"] = $"Editar: {dept.Name}";
        return View(new DepartmentViewModel
        {
            DepartmentId = dept.DepartmentId,
            Code = dept.Code,
            Name = dept.Name,
            Description = dept.Description,
            ManagerName = dept.ManagerName,
            IsActive = dept.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        if (await db.Departments.AnyAsync(d => d.Code == vm.Code && d.DepartmentId != id))
        {
            ModelState.AddModelError(nameof(vm.Code), "Ya existe otro departamento con ese código.");
            return View(vm);
        }

        dept.Code = vm.Code.Trim().ToUpper();
        dept.Name = vm.Name.Trim();
        dept.Description = vm.Description;
        dept.ManagerName = vm.ManagerName;
        dept.IsActive = vm.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = "Departamento actualizado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        ViewData["Title"] = $"Eliminar: {dept.Name}";
        return View(dept);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        var hasDocuments = await db.Documents.AnyAsync(d => d.DepartmentId == id);
        if (hasDocuments)
        {
            TempData["Error"] = "No se puede eliminar un departamento con documentos asociados. Desactívelo en su lugar.";
            return RedirectToAction(nameof(Index));
        }

        db.Departments.Remove(dept);
        await db.SaveChangesAsync();
        TempData["Success"] = "Departamento eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
