// Importuje klasę, która umożliwia utworzenie aplikacji funkcji w trybie Isolated Worker
using Microsoft.Azure.Functions.Worker;

// Importuje rozszerzenia do konfiguracji aplikacji funkcji
using Microsoft.Azure.Functions.Worker.Builder;

// Umożliwia korzystanie z wstrzykiwania zależności i konfiguracji usług
using Microsoft.Extensions.DependencyInjection;

// Udostępnia klasę do tworzenia i uruchamiania hosta aplikacji
using Microsoft.Extensions.Hosting;

// Udostępnia interfejs do logowania
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

// (Opcjonalnie: pozwala na lokalną konfigurację poziomu logowania)
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Wykonuje domyślną konfigurację dla funkcji HTTP, DI, serwera itp.
builder.ConfigureFunctionsWebApplication();

builder.Services
    // Włącza Application Insights (telemetria, logi w Azure)
    .AddApplicationInsightsTelemetryWorkerService()

    // Ustawia konfigurację Application Insights dla funkcji
    .ConfigureFunctionsApplicationInsights();

// Testowy log uruchomienia aplikacji
var tempLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
tempLogger.LogInformation("🚀 Azure Function host started (Program.cs log)");

builder.Build().Run(); // Buduje hosta i uruchamia aplikację
