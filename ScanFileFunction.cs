// 📦 Obsługa zdarzeń Event Grid
using Azure.Messaging.EventGrid;

// ⚙️ Atrybuty i wyzwalacze Azure Functions (Isolated Worker)
using Microsoft.Azure.Functions.Worker;

// 🧾 Logowanie do Application Insights lub lokalnie
using Microsoft.Extensions.Logging;

namespace ScanFileFunction
{
    public class ScanFileFunction
    {
        private readonly ILogger _logger;

        public ScanFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("ScanFileFunction");
        }

        [Function(nameof(ScanFileFunction))]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("🟢 ScanFileFunction was triggered!");
            _logger.LogInformation($"📦 Event type: {eventGridEvent.EventType}");
            _logger.LogInformation($"🧾 Event data: {eventGridEvent.Data.ToString()}");

            await Task.CompletedTask; // 👈 Minimalne async, żeby kompilator się nie czepiał
        }
    }
}
