using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;
using QualityDMS.Services;


namespace QualityDMS.Controllers;

[Authorize(Roles = "Admin,QualityManager,Auditor")]
public class AuditsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;

    public AuditsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog)
    {
        _db = db;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    // GET: /Audits
    public async Task<IActionResult> Index()
    {
        var audits = await _db.QualityAudits
            .Include(a => a.Department)
            .Include(a => a.LeadAuditor)
            .Include(a => a.Findings)
            .AsNoTracking()
            .OrderByDescending(a => a.PlannedStart)
            .ToListAsync();

        return View(audits);
    }

    // GET: /Audits/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var audit = await _db.QualityAudits
            .Include(a => a.Department)
            .Include(a => a.LeadAuditor)
            .Include(a => a.Findings)
                .ThenInclude(f => f.Responsible)
            .Include(a => a.Findings)
                .ThenInclude(f => f.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AuditId == id);

        if (audit is null) return NotFound();

        return View(audit);
    }

    // GET: /Audits/Create
    public async Task<IActionResult> Create()
    {
        var vm = new AuditCreateViewModel();
        await PopulateListsAsync(vm);
        return View(vm);
    }

    // POST: /Audits/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuditCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateListsAsync(vm);
            return View(vm);
        }

        if (vm.PlannedEnd < vm.PlannedStart)
        {
            ModelState.AddModelError("PlannedEnd", "La fecha de fin no puede ser anterior al inicio.");
            await PopulateListsAsync(vm);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var year = vm.PlannedStart.Year;
        var count = await _db.QualityAudits.CountAsync(a => a.PlannedStart.Year == year) + 1;
        var code = $"AUD-{year}-{count:D3}";

        var audit = new QualityAudit
        {
            Code = code,
            Title = vm.Title.Trim(),
            AuditType = vm.AuditType,
            Standard = vm.Standard,
            Scope = vm.Scope,
            DepartmentId = vm.DepartmentId,
            LeadAuditorId = vm.LeadAuditorId,
            PlannedStart = vm.PlannedStart,
            PlannedEnd = vm.PlannedEnd,
            Status = 1, // Planificada
            CreatedBy = userId
        };

        _db.QualityAudits.Add(audit);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync("Audit", audit.AuditId.ToString(), "Create", null, null, userId);

        TempData["Success"] = $"Auditoría {code} creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = audit.AuditId });
    }

    // POST: /Audits/AddFinding
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFinding(FindingCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = vm.AuditId });
        }

        var audit = await _db.QualityAudits
            .Include(a => a.Findings)
            .FirstOrDefaultAsync(a => a.AuditId == vm.AuditId);

        if (audit is null) return NotFound();

        var count = audit.Findings.Count + 1;
        var code = $"{audit.Code}-H{count:D2}";

        var finding = new AuditFinding
        {
            AuditId = vm.AuditId,
            Code = code,
            FindingType = vm.FindingType,
            Description = vm.Description.Trim(),
            Evidence = vm.Evidence?.Trim(),
            ClauseRef = vm.ClauseRef,
            DepartmentId = vm.DepartmentId,
            ResponsibleId = vm.ResponsibleId,
            DueDate = vm.DueDate,
            CorrectiveAction = vm.CorrectiveAction?.Trim(),
            Status = FindingStatus.Abierta
        };

        _db.AuditFindings.Add(finding);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Hallazgo {code} registrado.";
        return RedirectToAction(nameof(Details), new { id = vm.AuditId });
    }

    // POST: /Audits/CloseFinding/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseFinding(int findingId, string? verificationNote)
    {
        var finding = await _db.AuditFindings.FindAsync(findingId);
        if (finding is null) return NotFound();

        var userId = _userManager.GetUserId(User)!;

        finding.Status = FindingStatus.Cerrada;
        finding.VerifiedById = userId;
        finding.VerifiedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(verificationNote))
            finding.CorrectiveAction = (finding.CorrectiveAction ?? "") + "\n[Verificación]: " + verificationNote;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Hallazgo cerrado y verificado.";
        return RedirectToAction(nameof(Details), new { id = finding.AuditId });
    }

    // POST: /Audits/UpdateStatus
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, byte status)
    {
        var audit = await _db.QualityAudits.FindAsync(id);
        if (audit is null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var oldStatus = audit.Status;
        audit.Status = status;

        if (status == 2) audit.ActualStart = DateOnly.FromDateTime(DateTime.Today);
        if (status == 4) audit.ActualEnd = DateOnly.FromDateTime(DateTime.Today);

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Audit", id.ToString(), "StatusChange",
            $"{{\"Status\":{oldStatus}}}", $"{{\"Status\":{status}}}", userId);

        TempData["Success"] = "Estado de auditoría actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateListsAsync(AuditCreateViewModel vm)
    {
        var auditors = await _userManager.GetUsersInRoleAsync("Auditor");
        var qm = await _userManager.GetUsersInRoleAsync("QualityManager");
        var allAuditors = auditors.Concat(qm).DistinctBy(u => u.Id).ToList();

        vm.Auditors = new SelectList(allAuditors, "Id", "FullName", vm.LeadAuditorId);
        vm.Departments = new SelectList(
            await _db.Departments.Where(d => d.IsActive).ToListAsync(),
            "DepartmentId", "Name", vm.DepartmentId);
    }
}