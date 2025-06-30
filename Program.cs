// Importujemy przestrzenie nazw niezbędne do działania funkcji Azure
using Microsoft.Azure.Functions.Worker; // Główna przestrzeń do definiowania funkcji
using Microsoft.Azure.Functions.Worker.Builder; // Rozszerzenia do budowania aplikacji funkcji
using Microsoft.Extensions.DependencyInjection; // Obsługa Dependency Injection
using Microsoft.Extensions.Hosting; // Tworzenie i uruchamianie hosta aplikacji
using Microsoft.Extensions.Logging; // System logowania
using Microsoft.Extensions.Configuration; // Odczyt konfiguracji (np. z appsettings.json)

// Przestrzeń nazw zgodna z nazwą projektu
namespace ScanFileFunction
{
    // Główna klasa uruchamiająca aplikację funkcji
    public class Program
    {
        // Punkt wejścia do aplikacji — odpowiednik `Main()` w klasycznej aplikacji .NET
        public static void Main(string[] args)
        {
            // 🔧 Tworzymy buildera aplikacji funkcji (hosta) — odpowiada za konfigurację środowiska
            var builder = FunctionsApplication.CreateBuilder(args);

            // 🔧 (Opcjonalnie: tylko lokalnie) — umożliwia odczyt konfiguracji z appsettings.json
            // builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // 🔧 Domyślna konfiguracja aplikacji funkcji:
            // rejestruje obsługę funkcji, obsługę HTTP, DI i inne rozszerzenia
            builder.ConfigureFunctionsWebApplication();

            // 🔧 Rejestrujemy Application Insights (logi, metryki w Azure)
            builder.Services
                .AddApplicationInsightsTelemetryWorkerService() // Rejestracja telemetryki
                .ConfigureFunctionsApplicationInsights(); // Konfiguracja AI (np. sampling, context)

            // 🔧 Tworzymy tymczasowy logger, aby potwierdzić, że host funkcji wystartował
            var tempLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            tempLogger.LogInformation("🚀 Azure Function host started (Program.cs log)");

            // 🔧 Budujemy i uruchamiamy hosta aplikacji funkcji — od tego momentu działa nasza logika
            builder.Build().Run();
        }
    }
}
