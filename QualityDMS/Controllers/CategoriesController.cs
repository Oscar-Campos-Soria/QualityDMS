using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;

namespace QualityDMS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Categories
    public async Task<IActionResult> Index()
    {
        var categories = await _context.DocumentCategories
            .Include(c => c.Parent)
            .OrderBy(c => c.Code)
            .ToListAsync();
        return View(categories);
    }

    // GET: Categories/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Parents = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name");
        return View();
    }

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentCategory category)
    {
        if (ModelState.IsValid)
        {
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            _context.Add(category);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Categoría creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Parents = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name", category.ParentId);
        return View(category);
    }

    // GET: Categories/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.DocumentCategories.FindAsync(id);
        if (category == null) return NotFound();
        ViewBag.Parents = new SelectList(await _context.DocumentCategories.Where(c => c.CategoryId != id).ToListAsync(), "CategoryId", "Name", category.ParentId);
        return View(category);
    }

    // POST: Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DocumentCategory category)
    {
        if (id != category.CategoryId) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                category.UpdatedAt = DateTime.UtcNow;
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Categoría actualizada.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.DocumentCategories.Any(c => c.CategoryId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Parents = new SelectList(await _context.DocumentCategories.Where(c => c.CategoryId != id).ToListAsync(), "CategoryId", "Name", category.ParentId);
        return View(category);
    }

    // GET: Categories/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.DocumentCategories
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();
        return View(category);
    }

    // POST: Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.DocumentCategories.FindAsync(id);
        if (category != null)
        {
            _context.DocumentCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Categoría eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }
}