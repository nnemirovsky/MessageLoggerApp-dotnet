using MessageLoggerApp;
using MessageLoggerApp.Services;
using Serilog;

var hostBuilder = Host.CreateDefaultBuilder(args);

IConfiguration? config = null;

hostBuilder.ConfigureAppConfiguration(c => config = c.Build());

hostBuilder.ConfigureLogging(log =>
{
    var filePath = config!.GetSection("Logging").GetValue<string>("File");
    var outputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}, {SourceContext}] {Message}{NewLine}{Exception}";
    log.AddSerilog(new LoggerConfiguration().Enrich.FromLogContext().WriteTo
        .RollingFile(filePath, outputTemplate: outputTemplate).CreateLogger());
});

hostBuilder.ConfigureServices(services =>
{
    services.Configure<ServerConfiguration>(config!.GetSection("Server"));
    services.AddHostedService<LoginService>();
    services.AddHostedService<MessageLoggerService>();
});

var host = hostBuilder.Build();
await host.RunAsync();
