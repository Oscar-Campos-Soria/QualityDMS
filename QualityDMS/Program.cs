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
    // Contraseña
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    // Bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    // Usuario
    options.User.RequireUniqueEmail = true;
    // Email
    options.SignIn.RequireConfirmedEmail = false; // true en producción
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuración de cookies de autenticación
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

// Razor Runtime Compilation (útil en dev)
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

// ── CORS ─────────────────────────────────────────────────────
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
if (!builder.Environment.IsDevelopment())
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

// Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "documents",
    pattern: "docs/{action=Index}/{id?}",
    defaults: new { controller = "Documents" });

// ── Seed ──────────────────────────────────────────────────────
await SeedDatabaseAsync(app);

app.Run();

// ─────────────────────────────────────────────────────────────
// SEED: nunca insertar PasswordHash manualmente desde SQL.
// Identity usa PBKDF2 internamente; siempre usar CreateAsync /
// ResetPasswordAsync para generar hashes válidos.
// ─────────────────────────────────────────────────────────────
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const string adminEmail = "admin@qualitydms.com";
    const string adminPassword = "Admin@123456";
    string[] requiredRoles = ["Admin", "QualityManager", "DocumentOwner",
                                   "Reviewer", "Approver", "Reader", "Auditor"];
    string[] adminRoles = ["Admin", "QualityManager", "Approver"];

    try
    {
        await db.Database.MigrateAsync();

        // ── 1. Crear roles ────────────────────────────────────
        foreach (var role in requiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol creado: {Role}", role);
            }
        }

        // ── 2. Buscar usuario admin ───────────────────────────
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            // ── 2a. No existe: crear desde cero con hash válido
            var dept = db.Departments.FirstOrDefault(d => d.Code == "CAL");

            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador del Sistema",
                Position = "Administrador",
                DepartmentId = dept?.DepartmentId,
                EmailConfirmed = true,
                IsActive = true
            };

            // CreateAsync genera el hash PBKDF2 correctamente
            var createResult = await userManager.CreateAsync(admin, adminPassword);

            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, adminRoles);
                logger.LogInformation("Admin creado: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Error creando admin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // ── 2b. Ya existe: corregir hash inválido si viene del SQL seed ──
            // El script SQL insertó 'AQAAAAIAAYagAAAAEHashed_Password_Here'
            // que no es Base64 válido → System.FormatException en el login.
            bool hashInvalid = string.IsNullOrWhiteSpace(admin.PasswordHash)
                               || admin.PasswordHash.Contains("Hashed_Password_Here");

            if (hashInvalid)
            {
                logger.LogWarning("Hash inválido detectado para {Email}. Regenerando...", adminEmail);

                // ResetPasswordAsync genera un token seguro y actualiza el hash
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                var resetResult = await userManager.ResetPasswordAsync(admin, token, adminPassword);

                if (resetResult.Succeeded)
                    logger.LogInformation("Hash corregido correctamente para {Email}.", adminEmail);
                else
                    logger.LogError("Error al resetear contraseña: {Errors}",
                        string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }

            // ── 2c. Asegurarse de que tiene los roles requeridos ──
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