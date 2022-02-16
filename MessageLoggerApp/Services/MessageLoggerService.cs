using System.Net;
using System.Net.Sockets;
using MessageLoggerApp.Helpers;
using Microsoft.Extensions.Options;

namespace MessageLoggerApp.Services;

public class MessageLoggerService : BackgroundService
{
    private readonly ILogger<MessageLoggerService> _logger;
    private readonly IPEndPoint _ipEndPoint;
    private readonly string _salt;

    public MessageLoggerService(ILogger<MessageLoggerService> logger, IOptions<ServerConfiguration> serverConfiguration)
    {
        _logger = logger;
        _salt = serverConfiguration.Value.Salt;
        try
        {
            var address = IPAddress.Parse(serverConfiguration.Value.Address);
            _ipEndPoint = new IPEndPoint(address, serverConfiguration.Value.RcvMsgPort);
        }
        catch (Exception ex) when (ex is ArgumentNullException or FormatException)
        {
            _logger.LogCritical("IP address is in invalid format or null.");
            throw;
        }
        catch (ArgumentOutOfRangeException)
        {
            _logger.LogCritical("Message receiver server port out of range.");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service started.");
        var server = new TcpListener(_ipEndPoint);
        try
        {
            server.Start();
        }
        catch (SocketException)
        {
            _logger.LogCritical("Socket cannot be opened.");
            throw;
        }

        stoppingToken.Register(() =>
        {
            server.Stop();
            _logger.LogInformation("Service stopping...");
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await server.AcceptTcpClientAsync(stoppingToken);
            Task.Run(async () => await HandleRequest(client));
        }
    }

    private async Task HandleRequest(TcpClient client)
    {
        _logger.LogDebug("Handle message saving request");
        var stream = client.GetStream();
        var reader = new StreamReader(stream);
        var message = await reader.ReadLineAsync();
        var identity = await reader.ReadLineAsync();
        var token = await reader.ReadLineAsync();
        if (message is null || identity is null || token is null)
        {
            return;
        }

        var writer = new StreamWriter(stream);
        if (token != Hasher.GetHash(identity, _salt))
        {
            await writer.WriteLineAsync("Invalid token, sorry(((");
            await writer.FlushAsync();
            // _logger.LogInformation("Invalid token.");
            return;
        }

        _logger.LogInformation($"Message = '{message}'");
        await writer.WriteLineAsync("Message saved!");
        await writer.FlushAsync();
        stream.Close();
        client.Close();
    }
}
