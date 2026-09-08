namespace PropertyApi;

public class SwaggerEndpoints
{
    private readonly ISwashBuckleClient _swagger;

    public SwaggerEndpoints(ISwashBuckleClient swagger)
    {
        _swagger = swagger;
    }

    [SwaggerIgnore]
    [Function("SwaggerJson")]
    public Task<IActionResult> SwaggerJson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/json")] HttpRequest req)
        => _swagger.CreateSwaggerJsonDocumentResult(req);

    [SwaggerIgnore]
    [Function("SwaggerUi")]
    public Task<IActionResult> SwaggerUi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")] HttpRequest req)
        => _swagger.CreateSwaggerUIResult(req, "swagger/json");
}
