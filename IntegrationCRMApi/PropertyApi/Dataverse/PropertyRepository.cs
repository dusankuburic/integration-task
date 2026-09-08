using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PropertyApi.Common;
using PropertyApi.Exceptions;
using PropertyApi.Models;

namespace PropertyApi.Dataverse;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> GetActiveAsync(CancellationToken cancellationToken);
}

public class PropertyRepository : IPropertyRepository
{
    private const int ActiveStateCode = 0;
    private const int PageSize = 5000;

    private readonly ServiceClient _client;
    private readonly ILogger<PropertyRepository> _logger;

    public PropertyRepository(ServiceClient client, ILogger<PropertyRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Property>> GetActiveAsync(CancellationToken cancellationToken)
    {
        if (!_client.IsReady) {
            throw new DataverseUnavailableException(
                $"The Dataverse connection is not ready. {_client.LastError}".Trim());
        }

        var query = new QueryExpression(PropertyDv.EntityName);
        query.Distinct = true;
        query.NoLock = false;

        query.ColumnSet = new ColumnSet(
            PropertyDv.StateCode,
            PropertyDv.EntityId,
            PropertyDv.Name,
            PropertyDv.CreatedOn,
            PropertyDv.PropertyId,
            PropertyDv.Type,
            PropertyDv.NumberOfRooms,
            PropertyDv.Location,
            PropertyDv.AverageDailyRate,
            PropertyDv.RatingStars);

        query.Criteria = new FilterExpression(LogicalOperator.And);

        query.Criteria.Conditions.Add(
            new ConditionExpression(PropertyDv.StateCode, ConditionOperator.Equal, ActiveStateCode)
        );

        query.Orders.Add(new OrderExpression(PropertyDv.Name, OrderType.Ascending));
        query.Orders.Add(new OrderExpression(PropertyDv.EntityId, OrderType.Ascending));

        query.PageInfo = new PagingInfo {
            Count = PageSize,
            PageNumber = 1
        };

        var properties = new List<Property>();

        while (true) {
            var page = await _client.RetrieveMultipleAsync(query, cancellationToken);

            var mappeProperties = page.Entities.Select(Map);

            properties.AddRange(mappeProperties);

            if (!page.MoreRecords) {
                break;
            }

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = page.PagingCookie;
        }

        _logger.LogInformation(
            "Retrieved {Count} active {EntityName} rows across {Pages} page(s).",
            properties.Count,
            PropertyDv.EntityName,
            query.PageInfo.PageNumber);

        return properties.AsReadOnly();
    }

    private static Property Map(Entity entity) => new() {
        Id = entity.Id,
        PropertyId = EntityHelpers.GetString(entity, PropertyDv.PropertyId),
        Name = EntityHelpers.GetString(entity, PropertyDv.Name),
        PropertyType = EntityHelpers.GetString(entity, PropertyDv.Type),
        Location = EntityHelpers.GetString(entity, PropertyDv.Location),
        NumberOfRooms = entity.GetAttributeValue<int?>(PropertyDv.NumberOfRooms),
        AverageDailyRate = EntityHelpers.GetDecimal(entity, PropertyDv.AverageDailyRate),
        RatingStars = EntityHelpers.GetDecimal(entity, PropertyDv.RatingStars),
        StateCode = entity.GetAttributeValue<OptionSetValue>(PropertyDv.StateCode)?.Value,
        State = EntityHelpers.GetFormattedValue(entity, PropertyDv.StateCode),
        CreatedOn = entity.GetAttributeValue<DateTime?>(PropertyDv.CreatedOn)
    };
}
