using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Models.ViewModels;
using QualityDMS.Services;
using System.Text.Json;

namespace QualityDMS.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _storage;
    private readonly IWorkflowService _workflow;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notifications;
    private readonly ILogger<DocumentsController> _logger;

    private static readonly string[] AllowedExtensions =
    [
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt",
        ".dwg", ".dxf", ".png", ".jpg", ".jpeg", ".tiff", ".txt",
        ".csv", ".zip", ".rar"
    ];

    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    public DocumentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService storage,
        IWorkflowService workflow,
        IAuditLogService auditLog,
        INotificationService notifications,
        ILogger<DocumentsController> logger)
    {
        _db            = db;
        _userManager   = userManager;
        _storage       = storage;
        _workflow      = workflow;
        _auditLog      = auditLog;
        _notifications = notifications;
        _logger        = logger;
    }

    // ── GET /Documents ──────────────────────────────────────

    public async Task<IActionResult> Index(DocumentFilterViewModel filter)
    {
        var query = _db.Documents
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Owner)
            .Include(d => d.CurrentVersionNav)
            .AsNoTracking();

        if (!User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
        {
            var userId = _userManager.GetUserId(User)!;
            query = query.Where(d => !d.IsConfidential || d.OwnerId == userId);
        }

        if (filter.Status.HasValue)
            query = query.Where(d => d.CurrentStatus == filter.Status.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(d => d.CategoryId == filter.CategoryId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(d => d.DepartmentId == filter.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(d =>
                d.Title.Contains(term) ||
                d.Code.Contains(term) ||
                (d.Description != null && d.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DocumentSummaryDto
            {
                DocumentId     = d.DocumentId,
                Code           = d.Code,
                Title          = d.Title,
                CurrentStatus  = (byte)d.CurrentStatus,
                StatusLabel    = d.StatusLabel,
                StatusBadge    = d.StatusBadgeClass,
                CurrentVersion = d.CurrentVersion,
                CategoryName   = d.Category.Name,
                DepartmentName = d.Department.Name,
                OwnerName      = d.Owner.FullName,
                IsConfidential = d.IsConfidential,
                NextReviewDate = d.NextReviewDate,
                UpdatedAt      = d.UpdatedAt,
                FileExtension  = d.CurrentVersionNav != null ? d.CurrentVersionNav.FileExtension : ""
            })
            .ToListAsync();

        await PopulateFilterListsAsync(filter);

        var vm = new DocumentIndexViewModel
        {
            Documents   = documents,
            Filter      = filter,
            TotalCount  = totalCount,
            PageNumber  = filter.PageNumber,
            PageSize    = filter.PageSize
        };

        return View(vm);
    }

    // ── GET /Documents/Details/5 ────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var document = await _db.Documents
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Owner)
            .Include(d => d.Versions.OrderByDescending(v => v.CreatedAt))
                .ThenInclude(v => v.Author)
            .Include(d => d.Versions)
                .ThenInclude(v => v.ApprovedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == id);

        if (document is null) return NotFound();

        if (document.IsConfidential && !User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
        {
            var userId = _userManager.GetUserId(User)!;
            if (document.OwnerId != userId) return Forbid();
        }

        var activeWorkflow = await _db.WorkflowInstances
            .Include(w => w.Template)
                .ThenInclude(t => t!.Steps.OrderBy(s => s.StepOrder))
            .Include(w => w.Actions.OrderBy(a => a.ActedAt))
                .ThenInclude(a => a.Actor)
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.VersionId == (document.CurrentVersionId ?? 0) &&
                w.Status == WorkflowInstanceStatus.EnCurso);

        var userId2 = _userManager.GetUserId(User)!;
        var roles = await _userManager.GetRolesAsync(
            (await _userManager.FindByIdAsync(userId2))!);

        bool canApprove = false;
        PendingApprovalDto? pendingAction = null;

        if (activeWorkflow is not null)
        {
            var step = activeWorkflow.Template?.Steps
                .FirstOrDefault(s => s.StepOrder == activeWorkflow.CurrentStep);

            if (step is not null)
            {
                canApprove = step.AssigneeId == userId2 ||
                             (step.RoleRequired is not null && roles.Contains(step.RoleRequired));

                pendingAction = new PendingApprovalDto
                {
                    InstanceId     = activeWorkflow.InstanceId,
                    VersionId      = activeWorkflow.VersionId,
                    DocumentId     = id,
                    DocumentCode   = document.Code,
                    DocumentTitle  = document.Title,
                    StepName       = step.StepName,
                    StepType       = (byte)step.StepType,
                    CurrentStep    = activeWorkflow.CurrentStep,
                    StartedAt      = activeWorkflow.StartedAt,
                    DueDate        = activeWorkflow.StartedAt.AddDays(step.DaysAllowed)
                };
            }
        }

        var vm = new DocumentDetailViewModel
        {
            Document        = document,
            Versions        = document.Versions.ToList(),
            ActiveWorkflow  = activeWorkflow,
            WorkflowHistory = activeWorkflow?.Actions.ToList() ?? [],
            CanEdit         = document.CurrentStatus == DocumentStatus.Borrador &&
                              (document.OwnerId == userId2 || User.IsInRole("Admin")),
            CanUploadVersion = document.CurrentStatus is DocumentStatus.Borrador or DocumentStatus.Aprobado,
            CanApprove      = canApprove,
            CanObsolete     = document.CurrentStatus == DocumentStatus.Aprobado && User.IsInRole("QualityManager"),
            PendingAction   = pendingAction
        };

        return View(vm);
    }

    // ── GET /Documents/Create ───────────────────────────────

    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> Create()
    {
        var vm = new DocumentCreateViewModel();
        await PopulateCreateListsAsync(vm);
        return View(vm);
    }

    // ── POST /Documents/Create ──────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> Create(DocumentCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateListsAsync(vm);
            return View(vm);
        }

        var fileError = ValidateFile(vm.File);
        if (fileError is not null)
        {
            ModelState.AddModelError("File", fileError);
            await PopulateCreateListsAsync(vm);
            return View(vm);
        }

        if (await _db.Documents.AnyAsync(d => d.Code == vm.Code))
        {
            ModelState.AddModelError("Code", "Ya existe un documento con este código.");
            await PopulateCreateListsAsync(vm);
            return View(vm);
        }

        var userId          = _userManager.GetUserId(User)!;
        int createdDocumentId = 0;

        try
        {
            // ── Fix: ExecutionStrategy compatible con retry + transacción ──
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                var (fileName, filePath, hash) = await _storage.SaveFileAsync(vm.File!, "documents");

                var document = new Document
                {
                    Code           = vm.Code.ToUpper().Trim(),
                    Title          = vm.Title.Trim(),
                    Description    = vm.Description?.Trim(),
                    CategoryId     = vm.CategoryId,
                    DepartmentId   = vm.DepartmentId,
                    OwnerId        = userId,
                    CurrentStatus  = DocumentStatus.Borrador,
                    CurrentVersion = "0.1",
                    Tags           = vm.Tags,
                    IsConfidential = vm.IsConfidential,
                    NextReviewDate = vm.NextReviewDate,
                    CreatedBy      = userId,
                    UpdatedBy      = userId
                };

                _db.Documents.Add(document);
                await _db.SaveChangesAsync();

                var version = new DocumentVersion
                {
                    DocumentId       = document.DocumentId,
                    VersionNumber    = "0.1",
                    VersionType      = VersionType.Menor,
                    ChangeLog        = vm.ChangeLog ?? "Versión inicial",
                    FileName         = fileName,
                    OriginalFileName = vm.File!.FileName,
                    FilePath         = filePath,
                    FileExtension    = Path.GetExtension(vm.File.FileName).ToLower(),
                    FileSizeBytes    = vm.File.Length,
                    ContentType      = vm.File.ContentType,
                    FileHash         = hash,
                    Status           = DocumentStatus.Borrador,
                    AuthorId         = userId
                };

                _db.DocumentVersions.Add(version);
                await _db.SaveChangesAsync();

                document.CurrentVersionId = version.VersionId;
                await _db.SaveChangesAsync();

                await _workflow.StartWorkflowAsync(version.VersionId, vm.WorkflowTemplateId, userId);

                document.CurrentStatus = DocumentStatus.EnRevision;
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                createdDocumentId = document.DocumentId;

                await _auditLog.LogAsync("Document", document.DocumentId.ToString(),
                    "Create", null,
                    JsonSerializer.Serialize(new { document.Code, document.Title }),
                    userId);

                _logger.LogInformation("Documento {Code} creado por {UserId}", document.Code, userId);
                TempData["Success"] = $"Documento {document.Code} creado y enviado al flujo de aprobación.";
            });

            return RedirectToAction(nameof(Details), new { id = createdDocumentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando documento");
            ModelState.AddModelError("", "Ocurrió un error al crear el documento. Intente de nuevo.");
            await PopulateCreateListsAsync(vm);
            return View(vm);
        }
    }

    // ── GET /Documents/Edit/5 ───────────────────────────────

    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (document.OwnerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        if (document.CurrentStatus != DocumentStatus.Borrador)
        {
            TempData["Warning"] = "Solo se pueden editar documentos en estado Borrador.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new DocumentEditViewModel
        {
            DocumentId     = document.DocumentId,
            Title          = document.Title,
            Description    = document.Description,
            CategoryId     = document.CategoryId,
            DepartmentId   = document.DepartmentId,
            Tags           = document.Tags,
            IsConfidential = document.IsConfidential,
            NextReviewDate = document.NextReviewDate,
            Categories     = new SelectList(await _db.DocumentCategories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "Name", document.CategoryId),
            Departments    = new SelectList(await _db.Departments.Where(d => d.IsActive).ToListAsync(), "DepartmentId", "Name", document.DepartmentId)
        };

        return View(vm);
    }

    // ── POST /Documents/Edit/5 ──────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> Edit(int id, DocumentEditViewModel vm)
    {
        if (id != vm.DocumentId) return BadRequest();

        if (!ModelState.IsValid)
        {
            vm.Categories  = new SelectList(await _db.DocumentCategories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "Name", vm.CategoryId);
            vm.Departments = new SelectList(await _db.Departments.Where(d => d.IsActive).ToListAsync(), "DepartmentId", "Name", vm.DepartmentId);
            return View(vm);
        }

        var document = await _db.Documents.FindAsync(id);
        if (document is null) return NotFound();

        var userId    = _userManager.GetUserId(User)!;
        var oldValues = JsonSerializer.Serialize(new { document.Title, document.Description });

        document.Title          = vm.Title.Trim();
        document.Description    = vm.Description?.Trim();
        document.CategoryId     = vm.CategoryId;
        document.DepartmentId   = vm.DepartmentId;
        document.Tags           = vm.Tags;
        document.IsConfidential = vm.IsConfidential;
        document.NextReviewDate = vm.NextReviewDate;
        document.UpdatedAt      = DateTime.UtcNow;
        document.UpdatedBy      = userId;

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Document", id.ToString(), "Update",
            oldValues, JsonSerializer.Serialize(new { document.Title, document.Description }), userId);

        TempData["Success"] = "Documento actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── GET /Documents/NewVersion/5 ─────────────────────────

    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> NewVersion(int id)
    {
        var document = await _db.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.DocumentId == id);
        if (document is null) return NotFound();

        if (document.CurrentStatus != DocumentStatus.Aprobado)
        {
            TempData["Warning"] = "Solo se puede crear una nueva versión para documentos Aprobados.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new NewVersionViewModel
        {
            DocumentId        = id,
            DocumentCode      = document.Code,
            DocumentTitle     = document.Title,
            CurrentVersion    = document.CurrentVersion,
            ProposedVersion   = IncrementVersion(document.CurrentVersion, VersionType.Menor),
            WorkflowTemplates = new SelectList(
                await _db.WorkflowTemplates.Where(w => w.IsActive).ToListAsync(),
                "TemplateId", "Name")
        };

        return View(vm);
    }

    // ── POST /Documents/NewVersion ──────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,DocumentOwner,QualityManager")]
    public async Task<IActionResult> NewVersion(NewVersionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.WorkflowTemplates = new SelectList(
                await _db.WorkflowTemplates.Where(w => w.IsActive).ToListAsync(),
                "TemplateId", "Name");
            return View(vm);
        }

        var fileError = ValidateFile(vm.File);
        if (fileError is not null)
        {
            ModelState.AddModelError("File", fileError);
            vm.WorkflowTemplates = new SelectList(await _db.WorkflowTemplates.Where(w => w.IsActive).ToListAsync(), "TemplateId", "Name");
            return View(vm);
        }

        var document = await _db.Documents.FindAsync(vm.DocumentId);
        if (document is null) return NotFound();

        var userId           = _userManager.GetUserId(User)!;
        var newVersionNumber = IncrementVersion(document.CurrentVersion, vm.VersionType);

        try
        {
            // ── Fix: ExecutionStrategy compatible con retry + transacción ──
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                var (fileName, filePath, hash) = await _storage.SaveFileAsync(vm.File!, "documents");

                var version = new DocumentVersion
                {
                    DocumentId       = document.DocumentId,
                    VersionNumber    = newVersionNumber,
                    VersionType      = vm.VersionType,
                    ChangeLog        = vm.ChangeLog,
                    FileName         = fileName,
                    OriginalFileName = vm.File!.FileName,
                    FilePath         = filePath,
                    FileExtension    = Path.GetExtension(vm.File.FileName).ToLower(),
                    FileSizeBytes    = vm.File.Length,
                    ContentType      = vm.File.ContentType,
                    FileHash         = hash,
                    Status           = DocumentStatus.Borrador,
                    EffectiveDate    = vm.EffectiveDate,
                    AuthorId         = userId
                };

                _db.DocumentVersions.Add(version);
                await _db.SaveChangesAsync();

                await _workflow.StartWorkflowAsync(version.VersionId, vm.WorkflowTemplateId, userId);

                document.CurrentVersionId = version.VersionId;
                document.CurrentVersion   = newVersionNumber;
                document.CurrentStatus    = DocumentStatus.EnRevision;
                document.UpdatedAt        = DateTime.UtcNow;
                document.UpdatedBy        = userId;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Nueva versión {newVersionNumber} enviada al flujo de aprobación.";
            });

            return RedirectToAction(nameof(Details), new { id = document.DocumentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando nueva versión");
            ModelState.AddModelError("", "Error al crear la nueva versión.");
            vm.WorkflowTemplates = new SelectList(await _db.WorkflowTemplates.Where(w => w.IsActive).ToListAsync(), "TemplateId", "Name");
            return View(vm);
        }
    }

    // ── GET /Documents/Download/5 ───────────────────────────

    public async Task<IActionResult> Download(int id)
    {
        var version = await _db.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.VersionId == id);

        if (version is null) return NotFound();

        if (version.Document.IsConfidential && !User.IsInRole("Admin") && !User.IsInRole("QualityManager"))
        {
            var userId = _userManager.GetUserId(User)!;
            if (version.Document.OwnerId != userId) return Forbid();
        }

        var fileBytes = await _storage.GetFileAsync(version.FilePath);
        if (fileBytes is null) return NotFound("Archivo no encontrado en el servidor.");

        var userId2 = _userManager.GetUserId(User)!;
        await _auditLog.LogAsync("DocumentVersion", id.ToString(), "Download",
            null, null, userId2, additionalInfo: $"Descargó {version.OriginalFileName}");

        return File(fileBytes, version.ContentType, version.OriginalFileName);
    }

    // ── POST /Documents/Obsolete/5 ──────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,QualityManager")]
    public async Task<IActionResult> Obsolete(int id)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return NotFound();

        if (document.CurrentStatus != DocumentStatus.Aprobado)
        {
            TempData["Warning"] = "Solo se pueden obsoleter documentos Aprobados.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = _userManager.GetUserId(User)!;
        document.CurrentStatus = DocumentStatus.Obsoleto;
        document.UpdatedAt     = DateTime.UtcNow;
        document.UpdatedBy     = userId;

        if (document.CurrentVersionId.HasValue)
        {
            var v = await _db.DocumentVersions.FindAsync(document.CurrentVersionId.Value);
            if (v is not null)
            {
                v.Status      = DocumentStatus.Obsoleto;
                v.ObsoletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Document", id.ToString(), "Obsolete", null, null, userId);

        TempData["Success"] = "Documento marcado como Obsoleto.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Helpers ──────────────────────────────────────────────

    private static string? ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "El archivo es obligatorio.";

        if (file.Length > MaxFileSizeBytes)
            return $"El archivo excede el tamaño máximo de {MaxFileSizeBytes / 1024 / 1024} MB.";

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(ext))
            return $"Extensión no permitida. Permitidas: {string.Join(", ", AllowedExtensions)}";

        return null;
    }

    private static string IncrementVersion(string current, VersionType type)
    {
        var parts = current.Split('.');
        if (parts.Length < 2) return "1.0";

        if (!int.TryParse(parts[0], out var major)) major = 1;
        if (!int.TryParse(parts[1], out var minor)) minor = 0;

        return type switch
        {
            VersionType.Mayor  => $"{major + 1}.0",
            VersionType.Menor  => $"{major}.{minor + 1}",
            VersionType.Parche => $"{major}.{minor}.{(parts.Length > 2 && int.TryParse(parts[2], out var p) ? p + 1 : 1)}",
            _                  => $"{major}.{minor + 1}"
        };
    }

    private async Task PopulateFilterListsAsync(DocumentFilterViewModel filter)
    {
        filter.Categories = new SelectList(
            await _db.DocumentCategories.Where(c => c.IsActive && c.ParentId == null).ToListAsync(),
            "CategoryId", "Name", filter.CategoryId);

        filter.Departments = new SelectList(
            await _db.Departments.Where(d => d.IsActive).ToListAsync(),
            "DepartmentId", "Name", filter.DepartmentId);

        filter.Statuses = new SelectList(
            Enum.GetValues<DocumentStatus>()
                .Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text", filter.Status);
    }

    private async Task PopulateCreateListsAsync(DocumentCreateViewModel vm)
    {
        vm.Categories = new SelectList(
            await _db.DocumentCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
            "CategoryId", "Name", vm.CategoryId);

        vm.Departments = new SelectList(
            await _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
            "DepartmentId", "Name", vm.DepartmentId);

        vm.WorkflowTemplates = new SelectList(
            await _db.WorkflowTemplates.Where(w => w.IsActive).OrderByDescending(w => w.IsDefault).ToListAsync(),
            "TemplateId", "Name", vm.WorkflowTemplateId);
    }
}