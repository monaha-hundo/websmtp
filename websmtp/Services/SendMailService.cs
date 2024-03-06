
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
    private string DnsServer => _config.GetValue<string>("DNS:Ip") ?? throw new Exception("Missing DNS:Ip config key.");
    private int DnsPort => _config.GetValue<int>("DNS:Port");
    private int RemoteSmtpPort => _config.GetValue<int>("SMTP:RemotePort");

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

        if (signMessage)
        {
            Sign(message);
        }

        var destinationEmail = (message.To[0] as MailboxAddress)
            ?? throw new Exception("Could not parse destination email address.");

        var domain = destinationEmail.Domain
            ?? throw new Exception("Could not determine destination email adress domain.");

        _logger.LogDebug($"Mx Lookup for {domain}...");

        var ipEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(DnsServer), DnsPort);
        var lookup = new LookupClient(ipEndpoint);

        var response = domain == "localhost"
            ? new List<string>(1) { "localhost" }
            : LookUpEmailMxRecords(destinationEmail, lookup);

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
                if (exchangeRecord == "localhost")
                {
                    _logger.LogTrace($"Transfering email locally");
                    using var localClient = new SmtpClient();
                    localClient.Connect("127.0.0.1", 1025, SecureSocketOptions.None);
                    var localResult = localClient.Send(message);
                    return;
                }

                var exchange = exchangeRecord.TrimEnd('.');
                _logger.LogTrace($"Attempt #{attempts}: '{exchange}'.");
                using var client = new SmtpClient();
                client.Connect(exchange, RemoteSmtpPort, SecureSocketOptions.Auto); //SecureSocketOptions.StartTls
                var result = client.Send(message);
                _logger.LogDebug($"Attempt #{attempts}: sent through {exchange}.");
                _logger.LogTrace(result);

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Attempt #{attempts} failed: {ex.Message}.");
            }
        }

    }

    private void Sign(MimeMessage mimeMessage)
    {
        var headersToSign = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date };

        var domain = (mimeMessage.From[0] as MailboxAddress)?.Domain;
        var dkimDomainConfigSection = _config.GetSection("DKIM:Domains");
        var domainsConfigs = dkimDomainConfigSection.GetChildren();
        var domainConfig = domainsConfigs.Where(s => s.GetValue<string>("Name") == domain).SingleOrDefault();

        if (domainConfig == null) throw new Exception($"Trying to sign a message for an unconfigured email domain: '{domain}'.");

        var selector = domainConfig.GetValue<string>("Selector");
        var privateKeyFilename = domainConfig.GetValue<string>("PrivateKey");

        var signer = new DkimSigner(privateKeyFilename, domain, selector)
        {
            AgentOrUserIdentifier = $"@{domain}",
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
