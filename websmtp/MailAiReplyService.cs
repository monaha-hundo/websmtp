
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
    private readonly System.Timers.Timer _timer;
    private bool _working;

    public MailAiReplyService(ILogger<MailAiReplyService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _timer = new System.Timers.Timer(TimeSpan.FromSeconds(10));
        _timer.Elapsed += Timer_Elapsed;
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
            _logger.LogInformation($"No unreplied emails found. Nothing to do.");
            return;
        }

        _logger.LogInformation($"Found {emailsToReplyTo.Count} emails to reply to.");

        _working = true;
        foreach (var email in emailsToReplyTo)
        {
            _logger.LogInformation($"Processing email #{email.Id}...");
            var reply = SendReply(email);
            emails.MarkAsReplied(email, reply);
        }
        _working = false;
    }

    public MimeMessage SendReply(Message originalEmail)
    {
        _logger.LogInformation($"init message..");
        var message = new MimeMessage();

        string toName, toEmail, fromName, fromEmail;

        try
        {
            var regex = new Regex("(.{0,})\\<(.+)\\>");
            var fromResult = regex.Match(originalEmail.From);
            toName = fromResult.Groups[1].Value.Trim().Trim('"');
            toEmail = fromResult.Groups[2].Value.Trim().Trim('"');

            
            var toResult = regex.Match(originalEmail.To);
            fromName = toResult.Groups[1].Value.Trim().Trim('"');
            fromEmail = toResult.Groups[2].Value.Trim().Trim('"');
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Ex: {ex.Message}.");
            throw;
        }

        _logger.LogInformation($"init to mailBoxAddress... with '{toName}' + '{toEmail}'.");
        var to = new MailboxAddress(toName, toEmail);
        _logger.LogInformation($"add to address to message 'to' field.");
        message.To.Add(to);
        _logger.LogInformation($"add original originalEmail.To to address to message 'from' field.");
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        _logger.LogInformation($"set subject");
        message.Subject = $"RE: {originalEmail.Subject}";
        _logger.LogInformation($"set body");
        message.Body = new TextPart("plain")
        {
            @Text = "Let me think about it. (automatic reply by AI)"
        };

        _logger.LogInformation($"Mx Lookup for {to.Domain}...");
        var lookup = new LookupClient();
        var response = lookup.Query(to.Domain, QueryType.MX);
        _logger.LogInformation($"Responses: {response.Answers.Count}");

        var attempt = 0;
        foreach (MxRecord mxRecord in response.Answers)
        {
            attempt++;
            var exchange = mxRecord.Exchange.ToString().TrimEnd('.');
            _logger.LogInformation($"Attempt #{attempt}: '{exchange}'.");

            using var client = new SmtpClient();
            try
            {
                client.Connect(exchange, 25, SecureSocketOptions.StartTls);
                var result = client.Send(message);
                _logger.LogInformation($"Attempt #{attempt}: sent through {exchange}.");
                _logger.LogInformation(result);
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Attempt #{attempt} failed: {ex.Message}.");
            }
        }
        throw new Exception("Could not send AI generated reply.");
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");
        _timer.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
