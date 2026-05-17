namespace QualityDMS.Domain.Interfaces;

public interface IDocumentSearchService
{
    /// <summary>
    /// Returns doc_ids ordered by relevance. Throws on connectivity failure.
    /// </summary>
    Task<IReadOnlyList<int>> SearchDocumentIdsAsync(string query, CancellationToken ct = default);
}
