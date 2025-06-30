// 📦 Obsługa zdarzeń Event Grid
using Azure.Messaging.EventGrid;

// 📂 Klient do pracy z plikami w Azure Blob Storage
using Azure.Storage.Blobs;

// ⚙️ Atrybuty i wyzwalacze Azure Functions (Isolated Worker)
using Microsoft.Azure.Functions.Worker;

// 🧾 Logowanie do Application Insights lub lokalnie
using Microsoft.Extensions.Logging;

// 🌐 TCP do komunikacji z serwerem ClamAV
using System.Net;
using System.Net.Sockets;

namespace ScanFileFunction // ✅ Przestrzeń nazw projektu — upewnij się, że pasuje do reszty
{
    public class ScanFileFunction
    {
        private readonly ILogger _logger;

        // Konstruktor — inicjalizacja loggera z nadaną nazwą
        public ScanFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("ScanFileFunction");
        }

        // 🔁 Główna funkcja wyzwalana przez Event Grid
        [Function(nameof(ScanFileFunction))]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("🟢 ScanFileFunction was triggered.");
            _logger.LogInformation($"📦 Received event type: {eventGridEvent.EventType}");

            try
            {
                // 1️⃣ Pobranie adresu URL blobu ze zdarzenia Event Grid (typ-bezpiecznie)
                dynamic data = eventGridEvent.Data;
                string blobUrl = data?.url?.ToString() ?? throw new InvalidOperationException("❌ Blob URL is missing or invalid.");
                _logger.LogInformation($"🌐 Blob URL: {blobUrl}");

                // 2️⃣ Parsowanie URI i wyciągnięcie kontenera i nazwy blobu
                Uri uri = new(blobUrl);
                string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                {
                    _logger.LogWarning("⚠️ Unexpected blob URL format.");
                    return;
                }

                string containerName = segments[0];
                string blobName = string.Join('/', segments.Skip(1));
                _logger.LogInformation($"📄 Blob to scan: {containerName}/{blobName}");

                // 3️⃣ Pobranie pliku z kontenera uploads z Azure Blob Storage
                string blobConn = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
                if (string.IsNullOrEmpty(blobConn))
                    throw new InvalidOperationException("BlobStorageConnectionString is not set.");

                var blobClient = new BlobClient(blobConn, containerName, blobName);

                using MemoryStream ms = new();
                await blobClient.DownloadToAsync(ms);
                ms.Position = 0;
                _logger.LogInformation($"📥 Downloaded {ms.Length} bytes from blob.");

                // 4️⃣ Nawiązanie połączenia z ClamAV przez TCP
                string clamavHost = Environment.GetEnvironmentVariable("ClamAV_Host") ?? "localhost";
                int clamavPort = int.Parse(Environment.GetEnvironmentVariable("ClamAV_Port") ?? "3310");
                _logger.LogInformation($"🔌 Connecting to ClamAV at {clamavHost}:{clamavPort}");

                using TcpClient client = new(clamavHost, clamavPort);
                using NetworkStream stream = client.GetStream();

                // 5️⃣ Wysłanie nagłówka INSTREAM (rozpoczyna sesję skanowania)
                byte[] instream = System.Text.Encoding.ASCII.GetBytes("zINSTREAM\0");
                await stream.WriteAsync(instream);

                // 6️⃣ Wysyłanie danych w kawałkach do ClamAV (2 KB paczki)
                byte[] buffer = new byte[2048];
                int bytesRead;
                while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                    await stream.WriteAsync(lengthPrefix);
                    await stream.WriteAsync(buffer, 0, bytesRead);
                }

                // 7️⃣ Wysłanie pustego bloku (0 bajtów) kończącego przesyłanie
                await stream.WriteAsync(new byte[4]);

                // 8️⃣ Odczyt odpowiedzi od ClamAV
                using StreamReader reader = new(stream);
                string result = await reader.ReadToEndAsync();

                _logger.LogInformation($"🧪 ClamAV scan result: {result}");

                // 📌 (kolejny krok: EmitEventScanCompleted(result, blobName)...)
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error during scan: {ex.Message}");
            }
        }
    }
}
