// Importuje klasę, która umożliwia utworzenie aplikacji funkcji w trybie Isolated Worker
using Microsoft.Azure.Functions.Worker;

// Importuje rozszerzenia do konfiguracji aplikacji funkcji
using Microsoft.Azure.Functions.Worker.Builder;

// Umożliwia korzystanie z wstrzykiwania zależności i konfiguracji usług
using Microsoft.Extensions.DependencyInjection;

// Udostępnia klasę do tworzenia i uruchamiania hosta aplikacji
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Wykonuje domyślną konfigurację dla funkcji HTTP, DI, serwera itp.
builder.ConfigureFunctionsWebApplication();

builder.Services
    // Włącza Application Insights (telemetria, logi w Azure)
    .AddApplicationInsightsTelemetryWorkerService()
    
    // Ustawia konfigurację Application Insights dla funkcji
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run(); // Buduje hosta i uruchamia aplikację
