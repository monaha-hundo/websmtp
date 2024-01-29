using SmtpServer;

public class SmtpBackgroundServerService : IHostedService, IDisposable
{
    private readonly ILogger<SmtpBackgroundServerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SmtpServer.SmtpServer _smtpServer;

    public SmtpBackgroundServerService(ILogger<SmtpBackgroundServerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var options = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(1025)
            .Build();

        _smtpServer = new SmtpServer.SmtpServer(options, _serviceProvider);
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