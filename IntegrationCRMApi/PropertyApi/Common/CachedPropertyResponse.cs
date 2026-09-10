using PropertyApi.Models;

namespace PropertyApi.Common;

public class CachedPropertyResponse : IPropertyResponse
{
    private readonly IPropertyResponse _inner;
    private readonly HybridCache _cache;

    public CachedPropertyResponse(IPropertyResponse inner, HybridCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public ValueTask<PropertyPageJson> GetActiveJsonAsync(PageRequest page, CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            $"active_prop_json_p{page.Page}_s{page.Size}",
            page,
            _inner.GetActiveJsonAsync,
            cancellationToken: cancellationToken);
    }
}
