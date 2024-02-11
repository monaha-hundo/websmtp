using SmtpServer;

public class SmtpServerService : IHostedService, IDisposable
{
    private readonly ILogger<SmtpServerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SmtpServer.SmtpServer _smtpServer;

    public SmtpServerService(ILogger<SmtpServerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var hostEnv = _serviceProvider.GetRequiredService<IHostEnvironment>();

        var serverToServerOptions = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(hostEnv.IsProduction() ? 25 : 1025)
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