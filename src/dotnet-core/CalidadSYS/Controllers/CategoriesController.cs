using CalidadSYS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Infrastructure.Persistence;

namespace CalidadSYS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class CategoriesController(QualityDMSDbContext db) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
    {
        ViewData["Title"] = "Categorías de Documentos";
        var query = db.DocumentCategories
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name);

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

    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Nueva Categoría";
        return View(new CategoryViewModel
        {
            IsActive = true,
            ParentCategories = await GetParentCategoriesSelectList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        if (await db.DocumentCategories.AnyAsync(c => c.Code == vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "Ya existe una categoría con ese código.");
            vm.ParentCategories = await GetParentCategoriesSelectList();
            return View(vm);
        }

        db.DocumentCategories.Add(new DocumentCategory
        {
            Code = vm.Code.Trim().ToUpper(),
            Name = vm.Name.Trim(),
            Description = vm.Description,
            ParentCategoryId = vm.ParentCategoryId,
            IsActive = vm.IsActive
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Categoría creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cat = await db.DocumentCategories.FindAsync(id);
        if (cat is null) return NotFound();

        ViewData["Title"] = $"Editar: {cat.Name}";
        return View(new CategoryViewModel
        {
            CategoryId = cat.CategoryId,
            Code = cat.Code,
            Name = cat.Name,
            Description = cat.Description,
            ParentCategoryId = cat.ParentCategoryId,
            IsActive = cat.IsActive,
            ParentCategories = await GetParentCategoriesSelectList(cat.CategoryId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentCategories = await GetParentCategoriesSelectList(id);
            return View(vm);
        }

        var cat = await db.DocumentCategories.FindAsync(id);
        if (cat is null) return NotFound();

        if (await db.DocumentCategories.AnyAsync(c => c.Code == vm.Code && c.CategoryId != id))
        {
            ModelState.AddModelError(nameof(vm.Code), "Ya existe otra categoría con ese código.");
            vm.ParentCategories = await GetParentCategoriesSelectList(id);
            return View(vm);
        }

        cat.Code = vm.Code.Trim().ToUpper();
        cat.Name = vm.Name.Trim();
        cat.Description = vm.Description;
        cat.ParentCategoryId = vm.ParentCategoryId;
        cat.IsActive = vm.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = "Categoría actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.DocumentCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();

        ViewData["Title"] = $"Eliminar: {cat.Name}";
        return View(cat);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cat = await db.DocumentCategories.FindAsync(id);
        if (cat is null) return NotFound();

        var hasDocuments = await db.Documents.AnyAsync(d => d.CategoryId == id);
        if (hasDocuments)
        {
            TempData["Error"] = "No se puede eliminar una categoría con documentos. Desactívela en su lugar.";
            return RedirectToAction(nameof(Index));
        }

        db.DocumentCategories.Remove(cat);
        await db.SaveChangesAsync();
        TempData["Success"] = "Categoría eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetParentCategoriesSelectList(int excludeId = 0) =>
        (await db.DocumentCategories
            .Where(c => c.IsActive && c.CategoryId != excludeId)
            .OrderBy(c => c.Name)
            .ToListAsync())
        .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
}
