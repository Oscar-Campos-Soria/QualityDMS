using QualityDMS.Domain.Entities;

namespace QualityDMS.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken ct = default);
}
