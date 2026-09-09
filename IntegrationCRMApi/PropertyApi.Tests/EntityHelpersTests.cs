using Microsoft.Xrm.Sdk;
using PropertyApi.Common;
using Xunit;

namespace PropertyApi.Tests;

public class EntityHelpersTests
{
    [Fact]
    public void GetString_returns_null_when_the_attribute_is_missing()
    {
        var entity = new Entity(PropertyDv.EntityName);

        Assert.Null(EntityHelpers.GetString(entity, PropertyDv.Name));
    }

    [Fact]
    public void GetString_returns_the_text_of_a_string_attribute()
    {
        var entity = new Entity(PropertyDv.EntityName);
        entity[PropertyDv.Name] = "Seaside Villa";

        Assert.Equal("Seaside Villa", EntityHelpers.GetString(entity, PropertyDv.Name));
    }


    [Fact]
    public void GetString_returns_the_label_of_an_option_set()
    {
        var entity = new Entity(PropertyDv.EntityName);
        entity[PropertyDv.StateCode] = new OptionSetValue(0);
        entity.FormattedValues[PropertyDv.StateCode] = "Active";

        Assert.Equal("Active", EntityHelpers.GetString(entity, PropertyDv.StateCode));
    }

    [Fact]
    public void GetDecimal_unwraps_money()
    {
        var entity = new Entity(PropertyDv.EntityName);
        entity[PropertyDv.AverageDailyRate] = new Money(120.50m);

        Assert.Equal(120.50m, EntityHelpers.GetDecimal(entity, PropertyDv.AverageDailyRate));
    }

    [Fact]
    public void GetDecimal_returns_null_when_the_attribute_is_missing()
    {
        var entity = new Entity(PropertyDv.EntityName);

        Assert.Null(EntityHelpers.GetDecimal(entity, PropertyDv.RatingStars));
    }

    [Fact]
    public void GetDecimal_returns_null_for_a_type_it_cannot_convert()
    {
        var entity = new Entity(PropertyDv.EntityName);
        entity[PropertyDv.RatingStars] = "four and a half";

        Assert.Null(EntityHelpers.GetDecimal(entity, PropertyDv.RatingStars));
    }
}
