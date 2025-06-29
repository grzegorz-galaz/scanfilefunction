using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using ScanFileFunction.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<ClamAvScanner>()
    .AddSingleton(x =>
    {
        var config = x.GetRequiredService<IConfiguration>();
        var blobConnectionString = config["AzureWebJobsStorage"];
        return new BlobServiceClient(blobConnectionString);
    });

builder.Build().Run();
