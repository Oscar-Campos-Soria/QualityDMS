using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Data;
using QualityDMS.Models;
using QualityDMS.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Base de Datos ─────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3)
                  .CommandTimeout(60)));

// ── ASP.NET Core Identity ─────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});

if (builder.Environment.IsDevelopment())
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

// ── Servicios de Negocio ──────────────────────────────────────
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddHttpContextAccessor();

// ── Caché y Sesión ────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("InternalOnly", policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "https://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ── Antiforgery ───────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// ── Logging ───────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (!builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
    builder.Logging.AddEventLog();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("InternalOnly");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "documents",
    pattern: "docs/{action=Index}/{id?}",
    defaults: new { controller = "Documents" });

// ── Seed ──────────────────────────────────────────────────────
await SeedDatabaseAsync(app);
await SeedCatalogDataAsync(app);  // ← NUEVO

app.Run();

// ─────────────────────────────────────────────────────────────
// SeedDatabaseAsync: roles y usuario admin
// ─────────────────────────────────────────────────────────────
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const string adminEmail    = "admin@qualitydms.com";
    const string adminPassword = "Admin@123456";
    string[] requiredRoles = ["Admin", "QualityManager", "DocumentOwner",
                               "Reviewer", "Approver", "Reader", "Auditor"];
    string[] adminRoles    = ["Admin", "QualityManager", "Approver"];

    try
    {
        await db.Database.MigrateAsync();

        foreach (var role in requiredRoles)
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol creado: {Role}", role);
            }

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            var dept = db.Departments.FirstOrDefault(d => d.Code == "CAL");

            admin = new ApplicationUser
            {
                UserName        = adminEmail,
                Email           = adminEmail,
                FullName        = "Administrador del Sistema",
                Position        = "Administrador",
                DepartmentId    = dept?.DepartmentId,
                EmailConfirmed  = true,
                IsActive        = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, adminRoles);
                logger.LogInformation("Admin creado: {Email}", adminEmail);
            }
            else
                logger.LogError("Error creando admin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
        else
        {
            bool hashInvalid = string.IsNullOrWhiteSpace(admin.PasswordHash)
                               || admin.PasswordHash.Contains("Hashed_Password_Here");
            if (hashInvalid)
            {
                var token       = await userManager.GeneratePasswordResetTokenAsync(admin);
                var resetResult = await userManager.ResetPasswordAsync(admin, token, adminPassword);
                if (resetResult.Succeeded)
                    logger.LogInformation("Hash corregido para {Email}.", adminEmail);
                else
                    logger.LogError("Error al resetear contraseña: {Errors}",
                        string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }

            var currentRoles = await userManager.GetRolesAsync(admin);
            var missingRoles = adminRoles.Except(currentRoles).ToArray();
            if (missingRoles.Length > 0)
            {
                await userManager.AddToRolesAsync(admin, missingRoles);
                logger.LogInformation("Roles agregados a {Email}: {Roles}",
                    adminEmail, string.Join(", ", missingRoles));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante el seed de la base de datos");
    }
}

// ─────────────────────────────────────────────────────────────
// SeedCatalogDataAsync: departamentos, categorías y workflow
// ─────────────────────────────────────────────────────────────
static async Task SeedCatalogDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // ── Departamentos ─────────────────────────────────
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Code = "CAL", Name = "Calidad",          IsActive = true },
                new Department { Code = "OPE", Name = "Operaciones",      IsActive = true },
                new Department { Code = "RRH", Name = "Recursos Humanos", IsActive = true },
                new Department { Code = "TEC", Name = "Tecnología",       IsActive = true },
                new Department { Code = "FIN", Name = "Finanzas",         IsActive = true }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Departamentos creados.");
        }

        // ── Categorías de documentos ──────────────────────
        if (!await db.DocumentCategories.AnyAsync())
        {
            db.DocumentCategories.AddRange(
                new DocumentCategory { Code = "MAN", Name = "Manuales",       RetentionYears = 10, IsActive = true },
                new DocumentCategory { Code = "PRO", Name = "Procedimientos",  RetentionYears = 7,  IsActive = true },
                new DocumentCategory { Code = "REG", Name = "Registros",       RetentionYears = 5,  IsActive = true },
                new DocumentCategory { Code = "FOR", Name = "Formatos",        RetentionYears = 5,  IsActive = true },
                new DocumentCategory { Code = "INS", Name = "Instructivos",    RetentionYears = 7,  IsActive = true },
                new DocumentCategory { Code = "POL", Name = "Políticas",       RetentionYears = 10, IsActive = true }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Categorías creadas.");
        }

        // ── Workflow Template por defecto ─────────────────
        if (!await db.WorkflowTemplates.AnyAsync())
        {
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@qualitydms.com");
            if (admin is not null)
            {
                var template = new WorkflowTemplate
                {
                    Name        = "Flujo Estándar de Aprobación",
                    Description = "Revisión y aprobación estándar de documentos",
                    IsDefault   = true,
                    IsActive    = true,
                    CreatedBy   = admin.Id
                };
                db.WorkflowTemplates.Add(template);
                await db.SaveChangesAsync();

                db.WorkflowTemplateSteps.AddRange(
                    new WorkflowTemplateStep
                    {
                        TemplateId   = template.TemplateId,
                        StepOrder    = 1,
                        StepName     = "Revisión",
                        StepType     = WorkflowStepType.Revision,
                        RoleRequired = "Reviewer",
                        DaysAllowed  = 3,
                        IsRequired   = true
                    },
                    new WorkflowTemplateStep
                    {
                        TemplateId   = template.TemplateId,
                        StepOrder    = 2,
                        StepName     = "Aprobación",
                        StepType     = WorkflowStepType.Aprobacion,
                        RoleRequired = "Approver",
                        DaysAllowed  = 3,
                        IsRequired   = true
                    }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("Workflow template creado.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante el seed de catálogos");
    }
}