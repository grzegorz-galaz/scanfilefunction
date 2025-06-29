using Azure;
using Azure.Messaging.EventGrid;
using ScanFileFunction.Models;
using Microsoft.Extensions.Configuration;


namespace ScanFileFunction.Services;

public class EventPublisher
{
    private readonly EventGridPublisherClient _client;

    public EventPublisher(IConfiguration config)
    {
        _client = new EventGridPublisherClient(
            new Uri(config["EventGridTopicEndpoint"]),
            new AzureKeyCredential(config["EventGridKey"]));
    }

    public async Task PublishScanResultAsync(ScanResultEvent data)
    {
        var @event = new EventGridEvent(
            subject: $"scan/{data.FileName}",
            eventType: "ClamAV.ScanCompleted",
            dataVersion: "1.0",
            data: data);

        await _client.SendEventAsync(@event);
    }
}
