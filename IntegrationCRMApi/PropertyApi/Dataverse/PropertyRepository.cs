using PropertyApi.Common;
using PropertyApi.Exceptions;
using PropertyApi.Models;
using QueryExpression = Microsoft.Xrm.Sdk.Query.QueryExpression;

namespace PropertyApi.Dataverse;

public interface IPropertyRepository
{
    ValueTask<PropertyPage> GetActiveAsync(PageRequest page, CancellationToken cancellationToken);

    ValueTask<int> CountActiveAsync(CancellationToken cancellationToken);
}

public class PropertyRepository : IPropertyRepository
{
    private const int ActiveStateCode = 0;
    private const int CountPageSize = 5000;

    private readonly ServiceClient _client;
    private readonly ILogger<PropertyRepository> _logger;

    public PropertyRepository(ServiceClient client, ILogger<PropertyRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async ValueTask<PropertyPage> GetActiveAsync(PageRequest page, CancellationToken cancellationToken)
    {
        if (!_client.IsReady) {
            throw new DataverseUnavailableException(
                $"The Dataverse connection is not ready. {_client.LastError}".Trim());
        }

        var query = new QueryExpression(PropertyDv.EntityName);
        query.NoLock = false;

        query.ColumnSet = new ColumnSet(
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
        query.Orders.Add(new OrderExpression(PropertyDv.CreatedOn, OrderType.Ascending));

        query.Orders.Add(new OrderExpression(PropertyDv.EntityId, OrderType.Ascending));

        query.PageInfo = new PagingInfo {
            Count = page.Size,
            PageNumber = page.Page
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} active {EntityName} rows for page {Page} of size {Size}.",
            result.Entities.Count,
            PropertyDv.EntityName,
            page.Page,
            page.Size);

        return new PropertyPage {
            Items = result.Entities.Select(Map).ToList(),
            HasMore = result.MoreRecords
        };
    }

    public async ValueTask<int> CountActiveAsync(CancellationToken cancellationToken)
    {
        if (!_client.IsReady) {
            throw new DataverseUnavailableException(
                $"The Dataverse connection is not ready. {_client.LastError}".Trim());
        }

        var query = new QueryExpression(PropertyDv.EntityName);
        query.NoLock = false;
        query.ColumnSet = new ColumnSet(false);

        query.Criteria = new FilterExpression(LogicalOperator.And);

        query.Criteria.Conditions.Add(
            new ConditionExpression(PropertyDv.StateCode, ConditionOperator.Equal, ActiveStateCode)
        );

        query.Orders.Add(new OrderExpression(PropertyDv.EntityId, OrderType.Ascending));

        query.PageInfo = new PagingInfo {
            Count = CountPageSize,
            PageNumber = 1
        };

        var total = 0;

        while (true) {
            var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
            total += result.Entities.Count;

            if (!result.MoreRecords) {
                break;
            }

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = result.PagingCookie;
        }

        _logger.LogInformation(
            "Counted {Total} active {EntityName} rows over {Pages} pages.",
            total,
            PropertyDv.EntityName,
            query.PageInfo.PageNumber);

        return total;
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
        CreatedOn = entity.GetAttributeValue<DateTime?>(PropertyDv.CreatedOn)
    };
}
