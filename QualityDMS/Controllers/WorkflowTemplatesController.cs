using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;

namespace QualityDMS.Controllers;

[Authorize(Roles = "Admin,QualityManager")]
public class WorkflowTemplatesController : Controller
{
    private readonly ApplicationDbContext _context;

    public WorkflowTemplatesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: WorkflowTemplates
    public async Task<IActionResult> Index()
    {
        var templates = await _context.WorkflowTemplates
            .Include(t => t.Category)
            .Include(t => t.Steps)
            .OrderBy(t => t.Name)
            .ToListAsync();
        return View(templates);
    }

    // GET: WorkflowTemplates/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name");
        return View();
    }

    // POST: WorkflowTemplates/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkflowTemplate template)
    {
        if (ModelState.IsValid)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            template.CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            _context.Add(template);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Plantilla creada. Ahora puede agregar pasos.";
            return RedirectToAction(nameof(Edit), new { id = template.TemplateId });
        }
        ViewBag.Categories = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name", template.CategoryId);
        return View(template);
    }

    // GET: WorkflowTemplates/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var template = await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.TemplateId == id);
        if (template == null) return NotFound();

        ViewBag.Categories = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name", template.CategoryId);
        ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "FullName");
        return View(template);
    }

    // POST: WorkflowTemplates/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkflowTemplate template)
    {
        if (id != template.TemplateId) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                template.UpdatedAt = DateTime.UtcNow;
                _context.Update(template);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Plantilla actualizada.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.WorkflowTemplates.Any(t => t.TemplateId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Categories = new SelectList(await _context.DocumentCategories.ToListAsync(), "CategoryId", "Name", template.CategoryId);
        ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "FullName");
        return View(template);
    }

    // POST: WorkflowTemplates/AddStep
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStep(int templateId, WorkflowTemplateStep step)
    {
        if (ModelState.IsValid)
        {
            step.TemplateId = templateId;
            _context.WorkflowTemplateSteps.Add(step);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Paso agregado.";
        }
        return RedirectToAction(nameof(Edit), new { id = templateId });
    }

    // POST: WorkflowTemplates/RemoveStep
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStep(int stepId, int templateId)
    {
        var step = await _context.WorkflowTemplateSteps.FindAsync(stepId);
        if (step != null)
        {
            _context.WorkflowTemplateSteps.Remove(step);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Paso eliminado.";
        }
        return RedirectToAction(nameof(Edit), new { id = templateId });
    }

    // GET: WorkflowTemplates/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.TemplateId == id);
        if (template == null) return NotFound();
        return View(template);
    }

    // POST: WorkflowTemplates/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var template = await _context.WorkflowTemplates.FindAsync(id);
        if (template != null)
        {
            _context.WorkflowTemplates.Remove(template);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Plantilla eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }
}