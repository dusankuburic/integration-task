namespace PropertyApi.Models;

[ImmutableObject(true)]
public class PropertyPage
{
    public IReadOnlyList<Property> Items { get; init; }
    public bool HasMore { get; init; }
}
