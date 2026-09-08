using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using PropertyApi.Common;
using PropertyApi.Dataverse;

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

builder.Build().Run();
