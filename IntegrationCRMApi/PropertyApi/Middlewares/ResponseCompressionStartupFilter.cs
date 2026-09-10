namespace PropertyApi.Middlewares;

public class ResponseCompressionStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app => {
            app.UseResponseCompression();
            next(app);
        };
    }
}
