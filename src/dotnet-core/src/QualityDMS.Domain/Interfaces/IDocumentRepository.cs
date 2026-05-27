using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Document?> GetByIdWithVersionsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken ct = default);
    Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
        int? categoryId, int? departmentId, DocumentStatus? status,
        string? searchTerm, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Document>> GetExpiringAsync(int daysAhead, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    void Update(Document document);
    void Delete(Document document);
}
