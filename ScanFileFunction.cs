// Importujemy przestrzeń nazw do pracy ze zdarzeniami Event Grid
using Azure.Messaging.EventGrid;

// Importujemy atrybuty funkcji Azure Functions (w wersji isolated process)
using Microsoft.Azure.Functions.Worker;

// Importujemy loggera, który pozwala zapisywać logi (np. do Application Insights)
using Microsoft.Extensions.Logging;

namespace FileScanFunctions // Przestrzeń nazw projektu (może się różnić, ale trzymamy się tego schematu)
{
    // Klasa definiująca funkcję Azure Function — logikę, która będzie wywoływana po zdarzeniu z Event Grid
    public class ScanFileFunction
    {
        // Pole do przechowywania loggera przekazanego przez DI (Dependency Injection)
        private readonly ILogger<ScanFileFunction> _logger;

        // Konstruktor klasy, do którego wstrzykiwany jest logger — umożliwia logowanie zdarzeń
        public ScanFileFunction(ILogger<ScanFileFunction> logger)
        {
            _logger = logger;
        }

        // Główna funkcja, która zostanie wywołana, gdy Event Grid dostarczy zdarzenie
        [Function(nameof(ScanFileFunction))] // Atrybut określający nazwę funkcji w Azure
        public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            // Logujemy podstawowe informacje o odebranym zdarzeniu — typ oraz dane
            _logger.LogInformation($"Received event: {eventGridEvent.EventType}, Data: {eventGridEvent.Data.ToString()}");
        }
    }
}
