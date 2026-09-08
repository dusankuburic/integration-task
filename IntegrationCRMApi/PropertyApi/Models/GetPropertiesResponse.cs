namespace PropertyApi.Models;

public class GetPropertiesResponse
{
    public int Count { get; set; }
    public IReadOnlyList<Property> Properties { get; set; }
}