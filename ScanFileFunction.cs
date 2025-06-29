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
    private readonly BlobServiceClient _blobServiceClient;

    public ScanFileFunction(
        ILogger<ScanFileFunction> logger,
        ClamAvScanner scanner,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _scanner = scanner;
        _blobServiceClient = blobServiceClient;
    }

    [Function(nameof(ScanFileFunction))]
    public async Task Run([EventGridTrigger] EventGridEvent cloudEvent)
    {
        // Krok 1: Parsowanie URL z eventu
        var json = cloudEvent.Data.ToString();
        var eventData = JsonDocument.Parse(json);
        var url = eventData.RootElement.GetProperty("url").GetString();

        var uri = new Uri(url);
        var containerName = uri.Segments[1].TrimEnd('/');
        var fileName = Path.GetFileName(uri.LocalPath);

        _logger.LogInformation("Start scanning {FileName} from container {Container}", fileName, containerName);

        // Krok 2: Pobranie pliku
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        var stream = await blobClient.OpenReadAsync();

        // Krok 3: Skanowanie
        var result = await _scanner.ScanAsync(stream);

        _logger.LogInformation("Scan finished: {Status}  File: {FileName}",
            result.IsInfected ? "INFECTED" : "CLEAN", fileName);
    }
}
