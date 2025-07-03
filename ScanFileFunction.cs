using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.Text.Json;
using System.Net.Sockets;

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
            _logger.LogInformation("ScanFileFunction triggered.");

            try
            {
                var payload = eventGridEvent.Data.ToObjectFromJson<JsonElement>();
                _logger.LogInformation($"Payload raw data: {eventGridEvent.Data.ToString()}");

                if (!payload.TryGetProperty("url", out JsonElement urlElement) || urlElement.ValueKind != JsonValueKind.String)
                {
                    //throw new InvalidOperationException("Blob URL missing or invalid.");
                    _logger.LogError("Blob URL missing or invalid.");
                return;
                }
                string blobUrl = urlElement.GetString()!;
                _logger.LogInformation($"Blob URL: {blobUrl}");

                Uri uri = new(blobUrl);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                    throw new InvalidOperationException("Invalid blob URL format.");

                string container = segments[0];
                string blobName = string.Join('/', segments.Skip(1));
                _logger.LogInformation($"Parsed container name: {container}");
                _logger.LogInformation($"Parsed blob name: {blobName}");
                _logger.LogInformation($"Blob to scan: {container}/{blobName}");

                string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString")
                    ?? throw new InvalidOperationException("Missing BlobStorageConnectionString.");

                var blobClient = new BlobClient(connectionString, container, blobName);

                using var ms = new MemoryStream();
                _logger.LogInformation("Downloading blob...");
                try
                {
                    await blobClient.DownloadToAsync(ms);
                    _logger.LogInformation($"Downloaded {ms.Length} bytes");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to download blob: {ex}");
                    return;
                }
                ms.Position = 0;

                string clamHost = Environment.GetEnvironmentVariable("ClamAV_Host") ?? throw new InvalidOperationException("❌ Missing ClamAV_Host");
                if (!int.TryParse(Environment.GetEnvironmentVariable("ClamAV_Port"), out int clamPort))
                    throw new InvalidOperationException("Invalid or missing ClamAV_Port");

                _logger.LogInformation($"Connecting to ClamAV at {clamHost}:{clamPort}...");
                string scanResult = await ScanWithClamAV(clamHost, clamPort, ms);
                _logger.LogInformation($"Scan result: {scanResult}");

                if (scanResult.EndsWith("OK"))
                {
                    _logger.LogInformation("File is clean.");
                }
                else if (scanResult.Contains("FOUND"))
                {
                    _logger.LogWarning("Virus detected!");
                    await blobClient.DeleteIfExistsAsync();
                }
                else
                {
                    _logger.LogError($"Unexpected scan result: {scanResult}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ScanFileFunction: {ex}");
            }
        }

        private async Task<string> ScanWithClamAV(string host, int port, Stream fileStream)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port);
            using var networkStream = client.GetStream();

            byte[] instreamCommand = System.Text.Encoding.ASCII.GetBytes("zINSTREAM\0");
            await networkStream.WriteAsync(instreamCommand);

            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                byte[] sizePrefix = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(bytesRead));
                await networkStream.WriteAsync(sizePrefix, 0, 4);
                await networkStream.WriteAsync(buffer, 0, bytesRead);
            }

            byte[] zeroChunk = BitConverter.GetBytes(0);
            await networkStream.WriteAsync(zeroChunk, 0, 4);

            using var reader = new StreamReader(networkStream);
            string? result = await reader.ReadLineAsync();
            return result ?? "ERROR: No response from ClamAV";
        }
    }
}
