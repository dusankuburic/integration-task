using PropertyApi.Models;


namespace PropertyApi.Dataverse;

[ImmutableObject(true)]
public sealed record ActiveProperties(IReadOnlyList<Property> Items);


public class CachedPropertyRepository : IPropertyRepository
{
    private const string _key = "active_prop_key";

    private readonly IPropertyRepository _inner;
    private readonly HybridCache _cache;

    public CachedPropertyRepository(IPropertyRepository inner, HybridCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Property>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetOrCreateAsync(_key, LoadAsync, cancellationToken: cancellationToken);
        return cached.Items;
    }

    private async ValueTask<ActiveProperties> LoadAsync(CancellationToken cancellationToken)
    {
        var properties = await _inner.GetActiveAsync(cancellationToken);
        return new ActiveProperties(properties);
    }
}
