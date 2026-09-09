namespace PropertyApi.Common;

public static class EntityHelpers
{
    public static string GetString(Entity entity, string attribute)
    {
        if (!entity.Attributes.TryGetValue(attribute, out var value)) {
            return null;
        }

        return value switch {
            null => null,
            string text => text,
            OptionSetValue => GetFormattedValue(entity, attribute),
            _ => value.ToString()
        };
    }

    public static decimal? GetDecimal(Entity entity, string attribute)
    {
        if (!entity.Attributes.TryGetValue(attribute, out var value)) {
            return null;
        }

        return value switch {
            Money money => Normalize(money.Value),
            decimal number => Normalize(number),
            double number => Normalize((decimal)number),
            int number => number,
            long number => number,
            _ => null
        };
    }


    public static string GetFormattedValue(Entity entity, string attribute) =>
        entity.FormattedValues.TryGetValue(attribute, out var formatted) ? formatted : null;

    private static decimal Normalize(decimal value) => value / 1.000000000000000000000000000000000m;
}
