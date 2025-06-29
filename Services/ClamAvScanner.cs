using System.Net.Sockets;
using System.Net;

namespace ScanFileFunction.Services;

public class ClamAvScanner
{
    private readonly string _host;
    private readonly int _port;

    public ClamAvScanner(IConfiguration config)
    {
        _host = config["ClamAVHost"];
        _port = int.Parse(config["ClamAVPort"]);
    }

    public async Task<(bool IsInfected, string RawResponse)> ScanAsync(Stream input)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port);

        using var network = client.GetStream();
        var writer = new StreamWriter(network) { AutoFlush = true };
        await writer.WriteAsync("zINSTREAM\n");

        byte[] buffer = new byte[2048];
        int bytesRead;
        while ((bytesRead = await input.ReadAsync(buffer)) > 0)
        {
            var size = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
            await network.WriteAsync(size, 0, 4);
            await network.WriteAsync(buffer, 0, bytesRead);
        }

        var end = BitConverter.GetBytes(0);
        await network.WriteAsync(end, 0, 4);

        using var reader = new StreamReader(network);
        var response = await reader.ReadToEndAsync();

        return (response.Contains("FOUND"), response);
    }
}
