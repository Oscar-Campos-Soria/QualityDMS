using Npgsql;
using QualityDMS.Models;

namespace QualityDMS.Services;

// =========================================================================
// IPostgreSyncService
// =========================================================================
public interface IPostgreSyncService
{
    Task SyncApprovedDocumentAsync(Document document, DocumentVersion version);
    Task DeactivateDocumentAsync(string code);
}

// =========================================================================
// PostgreSyncService
// =========================================================================
public class PostgreSyncService : IPostgreSyncService
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSyncService> _logger;
    private readonly string _uploadsBasePath;

    public PostgreSyncService(IConfiguration config, ILogger<PostgreSyncService> logger)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")
            ?? throw new InvalidOperationException("PostgresConnection no configurada.");
        _uploadsBasePath  = config["FileStorage:BasePath"] ?? "C:\\QualityDMS\\uploads";
        _logger           = logger;
    }

    /// <summary>
    /// Inserta o actualiza un documento aprobado en PostgreSQL (publicdms.documents).
    /// </summary>
    public async Task SyncApprovedDocumentAsync(Document document, DocumentVersion version)
    {
        try
        {
            // Ruta relativa desde la carpeta uploads (lo que PHP necesita leer)
            var relativeFilePath = version.FilePath.Replace("\\", "/").TrimStart('/');

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // UPSERT: si el código ya existe lo actualiza, si no lo inserta
            const string sql = """
                INSERT INTO publicdms.documents
                    (code, title, version, category_name, department_name,
                     file_url, local_file_name, is_active, effective_date, last_sync)
                VALUES
                    (@code, @title, @version, @category, @department,
                     @fileUrl, @localName, true, @effectiveDate, NOW())
                ON CONFLICT (code) DO UPDATE SET
                    title           = EXCLUDED.title,
                    version         = EXCLUDED.version,
                    category_name   = EXCLUDED.category_name,
                    department_name = EXCLUDED.department_name,
                    file_url        = EXCLUDED.file_url,
                    local_file_name = EXCLUDED.local_file_name,
                    is_active       = true,
                    effective_date  = EXCLUDED.effective_date,
                    last_sync       = NOW();
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code",          document.Code);
            cmd.Parameters.AddWithValue("@title",         document.Title);
            cmd.Parameters.AddWithValue("@version",       version.VersionNumber);
            cmd.Parameters.AddWithValue("@category",      document.Category?.Name ?? "Sin categoría");
            cmd.Parameters.AddWithValue("@department",    document.Department?.Name ?? "Sin departamento");
            cmd.Parameters.AddWithValue("@fileUrl",       relativeFilePath);
            cmd.Parameters.AddWithValue("@localName",     version.OriginalFileName);
            cmd.Parameters.AddWithValue("@effectiveDate",
                version.EffectiveDate.HasValue
                    ? (object)version.EffectiveDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Documento {Code} sincronizado a PostgreSQL correctamente.", document.Code);
        }
        catch (Exception ex)
        {
            // El sync no debe detener el flujo principal
            _logger.LogError(ex,
                "Error al sincronizar documento {Code} a PostgreSQL.", document.Code);
        }
    }

    /// <summary>
    /// Marca un documento como inactivo en PostgreSQL (cuando se vuelve Obsoleto).
    /// </summary>
    public async Task DeactivateDocumentAsync(string code)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = """
                UPDATE publicdms.documents
                SET is_active = false, last_sync = NOW()
                WHERE code = @code;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", code);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Documento {Code} marcado como inactivo en PostgreSQL.", code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al desactivar documento {Code} en PostgreSQL.", code);
        }
    }
}