using SmtpServer;

namespace websmtp.services;

public class BackgroundSmtpServer : IHostedService, IDisposable
{
    private readonly ILogger<BackgroundSmtpServer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SmtpServer.SmtpServer _smtpServer;
    private readonly IConfiguration _config;

    public BackgroundSmtpServer(ILogger<BackgroundSmtpServer> logger,
        IServiceProvider serviceProvider,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;

        var smtpPort = _config.GetValue<int>("SMTP:Port");

        var serverToServerOptions = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Endpoint(ep => {
                ep
                  .Port(smtpPort, true)
                  .AllowUnsecureAuthentication(true);
            })
            .Build();

        _smtpServer = new SmtpServer.SmtpServer(serverToServerOptions, _serviceProvider);
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");
        _smtpServer.StartAsync(stoppingToken);
        return Task.CompletedTask;

    }


    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");
        _smtpServer.Shutdown();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}