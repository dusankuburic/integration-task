namespace PropertyApi.Models;

[Obsolete]
public class GetPropertiesResponse
{
    public IReadOnlyList<Property> Properties { get; set; }
}