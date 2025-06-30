// Importujemy przestrzeń nazw do pracy ze zdarzeniami Event Grid
using Azure.Messaging.EventGrid;

// Importujemy atrybuty funkcji Azure Functions (w wersji isolated process)
using Microsoft.Azure.Functions.Worker;

// Importujemy loggera, który pozwala zapisywać logi (np. do Application Insights)
using Microsoft.Extensions.Logging;

namespace ScanFileFunction // Upewnij się, że pasuje do reszty projektu
{
    public class ScanFileFunction
    {
        private readonly ILogger _logger;

        // Zamiast ILogger<T> — używamy ILoggerFactory i nadajemy własną nazwę loggerowi
        public ScanFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("ScanFileFunction");
        }

        [Function(nameof(ScanFileFunction))]
        public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("🟢 ScanFileFunction was triggered.");
            _logger.LogInformation($"📦 Received event type: {eventGridEvent.EventType}");
            _logger.LogInformation($"📄 Event data: {eventGridEvent.Data.ToString()}");
        }
    }
}
