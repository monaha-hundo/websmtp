
using System.Text.RegularExpressions;
using System.Timers;
using DnsClient;
using DnsClient.Protocol;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


namespace websmtp;

public class MailAiReplyService : IHostedService, IDisposable
{
    private readonly ILogger<MailAiReplyService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnv;
    private readonly System.Timers.Timer _timer;
    private bool _working;

    public MailAiReplyService(ILogger<MailAiReplyService> logger,
        IServiceProvider serviceProvider,
        IHostEnvironment hostEnv)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _timer = new System.Timers.Timer(TimeSpan.FromSeconds(10));
        _timer.Elapsed += Timer_Elapsed;
        _hostEnv = hostEnv;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_working)
        {
            _logger.LogInformation($"Already replying to emails... New emails will be processed later.");
            return;
        }

        var scope = _serviceProvider.CreateScope();
        var emails = scope.ServiceProvider.GetRequiredService<IReadableMessageStore>();
        var emailsToReplyTo = emails.UnReplied();

        if (!emailsToReplyTo.Any())
        {
            _logger.LogDebug($"No unreplied emails found. Nothing to do.");
            return;
        }

        _logger.LogInformation($"Found {emailsToReplyTo.Count} emails to reply to.");

        _working = true;
        foreach (var email in emailsToReplyTo)
        {
            _logger.LogTrace($"Processing email #{email.Id}...");
            var replyBytes = GenerateAndSendReply(email);
            emails.MarkAsReplied(email, replyBytes);
        }
        _working = false;
    }

    public byte[] GenerateAndSendReply(Message originalEmail)
    {
        using var ogEmailStream = new MemoryStream(originalEmail.Raw);
        using var ogEmail = MimeMessage.Load(ogEmailStream);

        var to = ogEmail.From[0] as MailboxAddress ?? throw new Exception("Invalid 'from' field in OG email.");
        var from = ogEmail.To[0] as MailboxAddress ?? throw new Exception("Invalid 'to' field in OG email."); ;

        using var message = new MimeMessage();
        message.To.Add(to);
        message.From.Add(from);
        message.Subject = $"RE: {originalEmail.Subject}";
        message.Body = new TextPart("plain")
        {
            @Text = "Let me think about it. (automatic reply by AI)"
        };

        _logger.LogDebug($"Mx Lookup for {to.Domain}...");
        var lookup = new LookupClient();
        var response = lookup.Query(to.Domain, QueryType.MX);
        _logger.LogDebug($"Responses: {response.Answers.Count}");

        return SendGeneratedReply(message, response);
    }

    private byte[] SendGeneratedReply(MimeMessage message, IDnsQueryResponse response)
    {
        var attempt = 0;
        foreach (MxRecord mxRecord in response.Answers)
        {
            attempt++;
            var exchange = mxRecord.Exchange.ToString().TrimEnd('.');
            _logger.LogTrace($"Attempt #{attempt}: '{exchange}'.");

            try
            {
                if (_hostEnv.IsProduction())
                {
                    using var client = new SmtpClient();
                    client.Connect(exchange, 25, SecureSocketOptions.StartTls);
                    var result = client.Send(message);
                    _logger.LogDebug($"Attempt #{attempt}: sent through {exchange}.");
                    _logger.LogTrace(result);
                }
                else
                {
                    _logger.LogDebug($"(development env.) Not sending email but proceeding as if it were.");
                }

                using var memory = new MemoryStream();
                message.WriteTo(memory);
                var bytes = memory.ToArray();

                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Attempt #{attempt} failed: {ex.Message}.");
            }
        }
        throw new Exception("Could not send generated reply.");
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Automatic Replies service.");
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Shutting down Automatic Replies service.");
        _timer.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer?.Dispose();
        }
    }
}
