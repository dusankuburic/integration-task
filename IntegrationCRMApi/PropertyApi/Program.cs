using PropertyApi.Common;
using PropertyApi.Dataverse;
using PropertyApi.Middlewares;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"))) {
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services.AddOptions<DataverseOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

string[] publicFunctions = ["SwaggerJson", "SwaggerUi"];

builder.UseWhen<AccessTokenMiddleware>(context =>
    !publicFunctions.Contains(context.FunctionDefinition.Name));

builder.Services.AddSingleton(provider => {
    var options = provider.GetRequiredService<IOptions<DataverseOptions>>().Value;
    var logger = provider.GetRequiredService<ILogger<ServiceClient>>();

    var client = new ServiceClient(
        new Uri(options.DataverseUrl),
        options.ClientId,
        options.ClientSecret,
        useUniqueInstance: false,
        logger);

    if (!client.IsReady)
        logger.LogError("Could not connect to Dataverse at {Url}. {Error}", options.DataverseUrl, client.LastError);

    return client;
});

var cacheMinutes = builder.Configuration.GetValue("CacheTTL", 5);
var cacheLifetime = TimeSpan.FromMinutes(cacheMinutes);

builder.Services.AddHybridCache(options => {
    options.DefaultEntryOptions = new HybridCacheEntryOptions {
        Expiration = cacheLifetime,
        LocalCacheExpiration = cacheLifetime
    };
});

const string dataverseRepositoryKey = "dataverse";

builder.Services.AddKeyedScoped<IPropertyRepository, PropertyRepository>(dataverseRepositoryKey);

builder.Services.AddScoped<IPropertyRepository>(provider =>
    new CachedPropertyRepository(
        provider.GetRequiredKeyedService<IPropertyRepository>(dataverseRepositoryKey),
        provider.GetRequiredService<HybridCache>()));

builder.Services.AddSwashBuckle(options => {
    options.RoutePrefix = "api";
    options.Title = "Property API";
    options.Documents = [
        new SwaggerDocument {
            Name = "v1",
            Title = "Property API",
            Description = "Access to properties",
            Version = "v1"
        }
    ];
    options.ConfigureSwaggerGen = swagger => {
        swagger.AddSecurityDefinition("bearer", new OpenApiSecurityScheme {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Configured access token sent as Bearer token"
        });

        swagger.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
    };
});

builder.Build().Run();
