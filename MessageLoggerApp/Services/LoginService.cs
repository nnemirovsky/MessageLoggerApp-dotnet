using System.Net;
using System.Net.Sockets;
using MessageLoggerApp.Helpers;
using Microsoft.Extensions.Options;

namespace MessageLoggerApp.Services;

public class LoginService : BackgroundService
{
    private readonly ILogger<LoginService> _logger;
    private readonly IPEndPoint _ipEndPoint;
    private readonly string _salt;

    public LoginService(ILogger<LoginService> logger, IOptions<ServerConfiguration> serverConfiguration)
    {
        _logger = logger;
        _salt = serverConfiguration.Value.Salt;
        try
        {
            var address = IPAddress.Parse(serverConfiguration.Value.Address);
            _ipEndPoint = new IPEndPoint(address, serverConfiguration.Value.LoginPort);
        }
        catch (Exception ex) when (ex is ArgumentNullException or FormatException)
        {
            _logger.LogCritical("IP address is in invalid format or null.");
            throw;
        }
        catch (ArgumentOutOfRangeException)
        {
            _logger.LogCritical("Login server port out of range.");
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
            // await HandleRequest(client);
        }
    }

    private async Task HandleRequest(TcpClient client)
    {
        _logger.LogDebug("Handle login request.");
        var stream = client.GetStream();
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream);
        var identity = await reader.ReadLineAsync();
        if (identity is null)
        {
            return;
        }

        var token = Hasher.GetHash(identity, _salt);
        // _logger.LogInformation(token);
        await writer.WriteLineAsync(token);
        await writer.FlushAsync();
        stream.Close();
        client.Close();
    }
}
