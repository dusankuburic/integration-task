namespace PropertyApi.Middlewares;

public class AccessTokenMiddleware : IFunctionsWorkerMiddleware
{
    private readonly string _expected;

    public AccessTokenMiddleware(IOptions<AuthOptions> options)
    {
        _expected = $"Bearer {options.Value.AccessToken}";
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();

        if (http is not null && http.Request.Headers.Authorization != _expected) {
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}


public class AuthOptions
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
