using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Infrastructure.Persistence;

public class UnitOfWork(QualityDMSDbContext ctx) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => ctx.SaveChangesAsync(ct);
}
