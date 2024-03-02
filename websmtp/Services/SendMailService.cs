﻿
using System.Text.RegularExpressions;
using System.Timers;
using DnsClient;
using DnsClient.Protocol;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Cryptography;


namespace websmtp;

public class SendMailService
{
    private readonly ILogger<SendMailService> _logger;
    private readonly IHostEnvironment _hostEnv;
    private readonly IConfiguration _config;

    public SendMailService(ILogger<SendMailService> logger,
        IHostEnvironment hostEnv,
        IConfiguration config)
    {
        _logger = logger;
        _hostEnv = hostEnv;
        _config = config;
    }

    public void SendMail(MimeMessage message)
    {
        var signMessage = _config.GetValue<bool>("DKIM:SigningEnabled");

        if(signMessage)
        {
            Sign(message);
        }

        var destinationEmail = (message.To[0] as MailboxAddress)
            ?? throw new Exception("Could not parse destination email address.");

        var domain = destinationEmail.Domain
            ?? throw new Exception("Could not determine destination email adress domain.");

        _logger.LogDebug($"Mx Lookup for {domain}...");

        var lookup = new LookupClient();
        var response = _hostEnv.IsProduction()
            ? LookUpEmailMxRecords(destinationEmail, lookup)
            : new List<string>(1) { "localhost" };

        _logger.LogDebug($"Responses: {response.Count}");

        if (response.Count == 0)
        {
            throw new Exception("No responses for MX record lookup.");
        }

        var attempts = 0;

        foreach (var exchangeRecord in response)
        {
            attempts++;

            try
            {
                if (_hostEnv.IsProduction())
                {
                    var exchange = exchangeRecord.TrimEnd('.');
                    _logger.LogTrace($"Attempt #{attempts}: '{exchange}'.");
                    using var client = new SmtpClient();
                    client.Connect(exchange, 25, SecureSocketOptions.StartTls);
                    var result = client.Send(message);
                    _logger.LogDebug($"Attempt #{attempts}: sent through {exchange}.");
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
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Attempt #{attempts} failed: {ex.Message}.");
            }
        }

    }

    private void Sign(MimeMessage mimeMessage)
    {
        var headersToSign =  new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date };

        var domain = _config.GetValue<string>("DKIM:Domain");
        var selector = _config.GetValue<string>("DKIM:selector");
        var privateKeyFilename = _config.GetValue<string>("DKIM:PrivateKey");

        var signer = new DkimSigner (privateKeyFilename, domain, selector) 
        {
            AgentOrUserIdentifier = "@skcr.me",
            QueryMethod = "dns/txt",
        };

        mimeMessage.Prepare(EncodingConstraint.SevenBit);

        signer.Sign(mimeMessage, headersToSign);
    }

    private static List<string> LookUpEmailMxRecords(MailboxAddress to, LookupClient lookup)
    {
        return lookup.Query(to.Domain, QueryType.MX).Answers
        .Select(anws => anws as MxRecord ?? throw new Exception("Answer was not an MX Record..."))
        .Select(anws => anws.Exchange.ToString())
        .ToList();
    }
}
