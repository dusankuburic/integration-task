using PropertyApi.Dataverse;
using PropertyApi.Exceptions;
using PropertyApi.Models;

namespace PropertyApi;

public class GetActiveProperties
{
    private readonly IPropertyRepository _properties;
    private readonly ILogger<GetActiveProperties> _logger;

    public GetActiveProperties(IPropertyRepository properties, ILogger<GetActiveProperties> logger)
    {
        _properties = properties;
        _logger = logger;
    }

    [Function("GetActiveProperties")]
    [ProducesResponseType(typeof(GetPropertiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "properties/active")] HttpRequest req,
        [SwaggerIgnore] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reading active properties from Dataverse.");

        try {
            var properties = await _properties.GetActiveAsync(cancellationToken);

            return new OkObjectResult(new GetPropertiesResponse {
                Count = properties.Count,
                Properties = properties
            });
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
