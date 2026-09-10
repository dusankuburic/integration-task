using PropertyApi.Dataverse;
using PropertyApi.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PropertyApi.Common;

public interface IPropertyResponse
{
    ValueTask<PropertyPageJson> GetActiveJsonAsync(PageRequest page, CancellationToken cancellationToken);
}

public class PropertyResponse : IPropertyResponse
{
    private readonly IPropertyRepository _properties;

    public PropertyResponse(IPropertyRepository properties)
    {
        _properties = properties;
    }

    public async ValueTask<PropertyPageJson> GetActiveJsonAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var result = await _properties.GetActiveAsync(page, cancellationToken);
        var total = await _properties.CountActiveAsync(cancellationToken);

        var payload = new PagedResult<Property> {
            Page = page.Page,
            PageSize = page.Size,
            Total = total,
            HasMore = result.HasMore,
            Items = result.Items
        };

        return new PropertyPageJson {
            Json = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            })
        };
    }
}
