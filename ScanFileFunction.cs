using System.Text.Json;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ScanFileFunction.Models;
using ScanFileFunction.Services;

namespace FileScanFunctions;

public class ScanFileFunction
{
    private readonly ILogger<ScanFileFunction> _logger;
    private readonly ClamAvScanner _scanner;
    private readonly EventPublisher _publisher;
    private readonly BlobServiceClient _blobServiceClient;

    public ScanFileFunction(
        ILogger<ScanFileFunction> logger,
        ClamAvScanner scanner,
        EventPublisher publisher,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _scanner = scanner;
        _publisher = publisher;
        _blobServiceClient = blobServiceClient;
    }

    [Function(nameof(ScanFileFunction))]
    public async Task Run([EventGridTrigger] EventGridEvent cloudEvent)
    {
        _logger.LogInformation("Received event: {Id}, Type: {Type}, Subject: {Subject}", 
            cloudEvent.Id, cloudEvent.EventType, cloudEvent.Subject);

        _logger.LogInformation("Raw data: {Data}", cloudEvent.Data.ToString());

        JsonDocument eventData;

        try
        {
            eventData = JsonDocument.Parse(cloudEvent.Data.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse cloudEvent.Data");
            return;
        }

        if (!eventData.RootElement.TryGetProperty("url", out var urlElement))
        {
            _logger.LogError("Property 'url' not found in event data.");
            return;
        }

        var url = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogError("URL is null or empty");
            return;
        }

        _logger.LogInformation("Parsed blob URL: {Url}", url);

        // Wyciąganie kontenera i pliku z URL
        Uri uri = new Uri(url);
        string containerName = uri.Segments[1].TrimEnd('/');
        string fileName = Path.GetFileName(uri.LocalPath);

        _logger.LogInformation("Start scanning file: {FileName} from container: {Container}", fileName, containerName);

        try
        {
            // Pobranie pliku z Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            var stream = await blobClient.OpenReadAsync();

            // Skanowanie pliku przez ClamAV
            var result = await _scanner.ScanAsync(stream);

            // Emitowanie eventu z wynikiem
            await _publisher.PublishScanResultAsync(new ScanResultEvent
            {
                FileName = fileName,
                Container = containerName,
                Status = result.IsInfected ? "infected" : "clean"
            });

            _logger.LogInformation("Scan finished: {Status} - File: {FileName}", 
                result.IsInfected ? "INFECTED" : "CLEAN", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during scan or publishing.");
        }
    }
}
