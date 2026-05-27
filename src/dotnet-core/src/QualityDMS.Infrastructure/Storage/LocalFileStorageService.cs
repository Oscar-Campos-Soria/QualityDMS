using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Infrastructure.Storage;

public class LocalFileStorageService(string basePath) : IFileStorageService
{
    public async Task<(string Path, long SizeBytes)> UploadAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // Forward slashes forzados — Path.Combine en Windows produce separadores mixtos
        var relativePath = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";

        var fullPath = Path.Combine(basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(fs, ct);

        return (relativePath, fs.Length);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);
        return await Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(basePath, path);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetDownloadUrl(string path) => $"/files/{path.Replace('\\', '/')}";
}
