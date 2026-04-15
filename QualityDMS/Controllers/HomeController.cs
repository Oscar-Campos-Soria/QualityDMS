using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;
using QualityDMS.Services;


namespace QualityDMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWorkflowService _workflow;

    public HomeController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWorkflowService workflow)
    {
        _db = db;
        _userManager = userManager;
        _workflow = workflow;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);

        // Métricas de documentos
        var docStats = await _db.Documents
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Borradores = g.Count(d => d.CurrentStatus == DocumentStatus.Borrador),
                EnRevision = g.Count(d => d.CurrentStatus == DocumentStatus.EnRevision),
                EnAprobacion = g.Count(d => d.CurrentStatus == DocumentStatus.EnAprobacion),
                Aprobados = g.Count(d => d.CurrentStatus == DocumentStatus.Aprobado),
                Obsoletos = g.Count(d => d.CurrentStatus == DocumentStatus.Obsoleto),
                PorVencer = g.Count(d =>
                    d.CurrentStatus == DocumentStatus.Aprobado &&
                    d.NextReviewDate != null &&
                    d.NextReviewDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(30)))
            })
            .FirstOrDefaultAsync();

        // Documentos por categoría
        var byCategory = await _db.Documents
            .Where(d => d.CurrentStatus == DocumentStatus.Aprobado)
            .GroupBy(d => d.Category.Name)
            .Select(g => new CategoryCountItem(g.Key, g.Count()))
            .ToListAsync();

        // Próximas revisiones
        var upcomingReviews = await _db.Documents
            .Where(d => d.CurrentStatus == DocumentStatus.Aprobado &&
                        d.NextReviewDate != null &&
                        d.NextReviewDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(30)))
            .Include(d => d.Owner)
            .Include(d => d.Category)
            .OrderBy(d => d.NextReviewDate)
            .Take(5)
            .Select(d => new DocumentSummaryDto
            {
                DocumentId = d.DocumentId,
                Code = d.Code,
                Title = d.Title,
                CurrentVersion = d.CurrentVersion,
                NextReviewDate = d.NextReviewDate,
                CategoryName = d.Category.Name,
                OwnerName = d.Owner.FullName
            })
            .ToListAsync();

        // Pendientes de aprobación
        var pending = await _workflow.GetPendingApprovalsAsync(user.Id, roles);

        // Notificaciones no leídas
        var notifications = await _db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalDocuments = docStats?.Total ?? 0,
            Borradores = docStats?.Borradores ?? 0,
            EnRevision = docStats?.EnRevision ?? 0,
            EnAprobacion = docStats?.EnAprobacion ?? 0,
            Aprobados = docStats?.Aprobados ?? 0,
            Obsoletos = docStats?.Obsoletos ?? 0,
            PorVencer = docStats?.PorVencer ?? 0,
            DocumentsByCategory = byCategory,
            UpcomingReviews = upcomingReviews,
            PendingApprovals = pending,
            RecentNotifications = notifications,
            UnreadNotifications = notifications.Count(n => !n.IsRead)
        };

        return View(vm);
    }

    // GET: /Home/Search
    public async Task<IActionResult> Search(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View(new GlobalSearchViewModel { Query = q });

        var term = q.Trim();

        var docs = await _db.Documents
            .Where(d => d.Title.Contains(term) || d.Code.Contains(term) ||
                        (d.Description != null && d.Description.Contains(term)))
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Owner)
            .OrderByDescending(d => d.UpdatedAt)
            .Take(20)
            .Select(d => new DocumentSummaryDto
            {
                DocumentId = d.DocumentId,
                Code = d.Code,
                Title = d.Title,
                StatusLabel = d.StatusLabel,
                StatusBadge = d.StatusBadgeClass,
                CurrentVersion = d.CurrentVersion,
                CategoryName = d.Category.Name,
                DepartmentName = d.Department.Name,
                OwnerName = d.Owner.FullName,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        var audits = await _db.QualityAudits
            .Where(a => a.Title.Contains(term) || a.Code.Contains(term))
            .Include(a => a.Department)
            .OrderByDescending(a => a.PlannedStart)
            .Take(10)
            .ToListAsync();

        return View(new GlobalSearchViewModel
        {
            Query = q,
            Documents = docs,
            Audits = audits
        });
    }

    // POST: /Home/MarkNotificationRead
    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

        if (notif is not null)
        {
            notif.IsRead = true;
            notif.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}