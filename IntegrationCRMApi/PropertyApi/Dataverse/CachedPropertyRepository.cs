using PropertyApi.Common;
using PropertyApi.Models;

namespace PropertyApi.Dataverse;

public class CachedPropertyRepository : IPropertyRepository
{
    private readonly IPropertyRepository _inner;
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _countOptions;

    public CachedPropertyRepository(IPropertyRepository inner, HybridCache cache, TimeSpan countLifetime)
    {
        _inner = inner;
        _cache = cache;

        _countOptions = new HybridCacheEntryOptions {
            Expiration = countLifetime,
            LocalCacheExpiration = countLifetime
        };
    }

    public ValueTask<PropertyPage> GetActiveAsync(PageRequest page, CancellationToken cancellationToken)
    {
        return _inner.GetActiveAsync(page, cancellationToken);
    }

    public ValueTask<int> CountActiveAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            "active_prop_count",
            _inner.CountActiveAsync,
            _countOptions,
            cancellationToken: cancellationToken);
    }
}
