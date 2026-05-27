namespace QualityDMS.Domain.Interfaces;

public interface IFileStorageService
{
    Task<(string Path, long SizeBytes)> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    string GetDownloadUrl(string path);
}
