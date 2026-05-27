namespace QualityDMS.Application.Common.Interfaces;

public interface ICacheableQuery
{
    string CacheKey { get; }
    int CacheDurationMinutes { get; }
}
