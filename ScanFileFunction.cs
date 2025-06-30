using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using nClam;
using System.Text.Json;
using System.Net;

namespace ScanFileFunction
{
    public class ScanFileFunction
    {
        private readonly ILogger _logger;

        public ScanFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScanFileFunction>();
        }

        [Function("ScanFileFunction")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("🔔 ScanFileFunction triggered");

            try
            {
                var payload = eventGridEvent.Data.ToObjectFromJson<JsonElement>();

                if (!payload.TryGetProperty("url", out JsonElement urlElement) || urlElement.ValueKind != JsonValueKind.String)
                    throw new InvalidOperationException("❌ Blob URL missing or invalid.");

                string blobUrl = urlElement.GetString()!;
                _logger.LogInformation($"🌐 Blob URL: {blobUrl}");

                Uri uri = new(blobUrl);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                    throw new InvalidOperationException("❌ Invalid blob URL format.");

                string container = segments[0];
                string blobName = string.Join('/', segments.Skip(1));

                _logger.LogInformation($"📄 Blob to scan: {container}/{blobName}");

                string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString")
                    ?? throw new InvalidOperationException("Missing BlobStorageConnectionString");

                var blobClient = new BlobClient(connectionString, container, blobName);

                using var ms = new MemoryStream();
                await blobClient.DownloadToAsync(ms);
                _logger.LogInformation($"📥 Downloaded {ms.Length} bytes");

                ms.Position = 0;

                string clamHost = Environment.GetEnvironmentVariable("ClamAV_Host") ?? "localhost";
                int clamPort = int.Parse(Environment.GetEnvironmentVariable("ClamAV_Port") ?? "3310");

                var clam = new ClamClient(clamHost, clamPort);
                if (!await clam.PingAsync())
                    throw new InvalidOperationException("❌ ClamAV server unreachable");

                var scanResult = await clam.SendAndScanFileAsync(ms.ToArray());

                switch (scanResult.Result)
                {
                    case ClamScanResults.Clean:
                        _logger.LogInformation("✅ File is clean.");
                        break;
                    case ClamScanResults.VirusDetected:
                        _logger.LogWarning($"🦠 Virus found: {scanResult.InfectedFiles?.FirstOrDefault()?.VirusName}");
                        await blobClient.DeleteIfExistsAsync();
                        break;
                    default:
                        _logger.LogError("⚠️ Error during scan.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error in function: {ex.Message}");
            }
        }
    }
}
