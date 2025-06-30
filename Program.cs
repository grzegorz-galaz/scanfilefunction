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

// Udostępnia możliwość odczytu konfiguracji (np. z appsettings.json)
using Microsoft.Extensions.Configuration;

// 🔧 Tworzymy buildera aplikacji funkcji
var builder = FunctionsApplication.CreateBuilder(args);

// 🔧 (Opcjonalnie, tylko lokalnie): pozwala na użycie pliku appsettings.json do konfiguracji
// builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// 🔧 Konfigurujemy domyślne ustawienia funkcji (obsługa HTTP, DI, itp.)
builder.ConfigureFunctionsWebApplication();

builder.Services
    // 🔧 Włączamy Application Insights — telemetria, logi w Azure
    .AddApplicationInsightsTelemetryWorkerService()

    // 🔧 Konfigurujemy ustawienia Application Insights dla funkcji
    .ConfigureFunctionsApplicationInsights();

// 🔧 Log startowy — pomocny, aby upewnić się, że host wystartował
var tempLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
tempLogger.LogInformation("🚀 Azure Function host started (Program.cs log)");

// 🔧 Budujemy i uruchamiamy hosta aplikacji funkcji
builder.Build().Run();
