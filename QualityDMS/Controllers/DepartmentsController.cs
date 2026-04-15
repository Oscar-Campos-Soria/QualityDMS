using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;

namespace QualityDMS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Departments
    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments.OrderBy(d => d.Code).ToListAsync();
        return View(departments);
    }

    // GET: Departments/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Departments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (ModelState.IsValid)
        {
            department.CreatedAt = DateTime.UtcNow;
            department.UpdatedAt = DateTime.UtcNow;
            _context.Add(department);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Departamento creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    // GET: Departments/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();
        return View(department);
    }

    // POST: Departments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department department)
    {
        if (id != department.DepartmentId) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                department.UpdatedAt = DateTime.UtcNow;
                _context.Update(department);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Departamento actualizado.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Departments.Any(d => d.DepartmentId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    // GET: Departments/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
        if (department == null) return NotFound();
        return View(department);
    }

    // POST: Departments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Departamento eliminado.";
        }
        return RedirectToAction(nameof(Index));
    }
}