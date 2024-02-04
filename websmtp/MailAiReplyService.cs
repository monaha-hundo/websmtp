
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
        try
        {
            foreach (var email in emailsToReplyTo)
            {
                _logger.LogTrace($"Processing email #{email.Id}...");
                GenerateAndSendReply(email);
                emails.SaveMessage(email);
            }
        }
        finally
        {
            _working = false;
        }
    }

    public void GenerateAndSendReply(Message originalEmail)
    {
        using var ogEmailStream = new MemoryStream(originalEmail.Raw);
        using var ogEmail = MimeMessage.Load(ogEmailStream);

        var to = ogEmail.From[0] as MailboxAddress ?? throw new Exception("Invalid 'from' field in OG email.");
        var from = ogEmail.To[0] as MailboxAddress ?? throw new Exception("Invalid 'to' field in OG email."); ;
        var subject = $"RE: {originalEmail.Subject}";
        var text = "Let me think about it. (automatic reply by AI)";

        using var message = new MimeMessage();
        message.To.Add(to);
        message.From.Add(from);
        message.Subject = subject;
        message.Body = new TextPart("plain") { @Text = text };

        originalEmail.ReplySubject = subject;
        originalEmail.ReplyText = text;

        _logger.LogDebug($"Mx Lookup for {to.Domain}...");

        var lookup = new LookupClient();
        var response = _hostEnv.IsProduction()
            ? LookUpEmailMxRecords(to, lookup)
            : new List<string>(1) { "localhost" };

        _logger.LogDebug($"Responses: {response.Count}");

        SendGeneratedReply(originalEmail, message, response);
    }

    private static List<string> LookUpEmailMxRecords(MailboxAddress to, LookupClient lookup)
    {
        return lookup.Query(to.Domain, QueryType.MX).Answers
        .Select(anws => anws as MxRecord ?? throw new Exception("Answer was not an MX Record..."))
        .Select(anws => anws.Exchange.ToString())
        .ToList();
    }

    private void SendGeneratedReply(Message originalEmail, MimeMessage message, List<string> response)
    {
        var attempt = 0;
        foreach (var exchangeRecord in response)
        {
            attempt++;

            try
            {
                if (_hostEnv.IsProduction())
                {
                    var exchange = exchangeRecord.TrimEnd('.');
                    _logger.LogTrace($"Attempt #{attempt}: '{exchange}'.");
                    using var client = new SmtpClient();
                    client.Connect(exchange, 25, SecureSocketOptions.StartTls);
                    var result = client.Send(message);
                    _logger.LogDebug($"Attempt #{attempt}: sent through {exchange}.");
                    _logger.LogTrace(result);
                }
                else if (_hostEnv.IsDevelopment() && exchangeRecord == "localhost")
                {
                    _logger.LogTrace($"Transfering email locally");
                    using var client = new SmtpClient();
                    client.Connect("127.0.0.1", 1025, SecureSocketOptions.None);
                    var result = client.Send(message);
                }
                else
                {
                    _logger.LogDebug($"(development env.) Not sending email but proceeding as if it were.");
                }


                // using var memory = new MemoryStream();
                // message.WriteTo(memory);
                // var bytes = memory.ToArray();
                //return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Attempt #{attempt} failed: {ex.Message}.");
            }
        }
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
