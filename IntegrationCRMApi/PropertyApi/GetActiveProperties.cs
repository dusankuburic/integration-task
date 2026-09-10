using PropertyApi.Common;
using PropertyApi.Exceptions;
using PropertyApi.Models;

namespace PropertyApi;

public class GetActiveProperties
{
    private readonly IPropertyResponse _properties;
    private readonly ILogger<GetActiveProperties> _logger;

    public GetActiveProperties(IPropertyResponse properties, ILogger<GetActiveProperties> logger)
    {
        _properties = properties;
        _logger = logger;
    }

    [Function("GetActiveProperties")]
    [QueryStringParameter("page", "1-based page number", DataType = typeof(int), Required = false)]
    [QueryStringParameter("pageSize", "Rows per page, 1 to 1000", DataType = typeof(int), Required = false)]
    [ProducesResponseType(typeof(PagedResult<Property>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "properties/active")] HttpRequest req,
        [SwaggerIgnore] CancellationToken cancellationToken)
    {
        if (!req.TryGetPage(out var page, out var error)) {
            return new BadRequestObjectResult(new ProblemDetails {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid paging",
                Detail = error
            });
        }

        _logger.LogInformation("Reading active properties from Dataverse.");

        try {
            var response = await _properties.GetActiveJsonAsync(page, cancellationToken);
            return new FileContentResult(response.Json, "application/json; charset=utf-8");
        }
        catch (DataverseUnavailableException ex) {
            _logger.LogError(ex, "Dataverse is unavailable.");

            return new ObjectResult(new ProblemDetails {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Dataverse is unavailable",
                Detail = ex.Message
            }) {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            _logger.LogInformation("The request was cancelled before the properties were returned.");
            return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to read active properties from Dataverse.");

            return new ObjectResult(new ProblemDetails {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Failed to read active properties"
            }) {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
