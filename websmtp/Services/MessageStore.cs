using DnsClient;
using DnsClient.Protocol;
using MimeKit;
using MimeKit.Cryptography;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Net;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using SQLitePCL;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp;

public enum SpfVerifyResult
{
    None,
    Neutral,
    Pass,
    Fail,
    Softfail,
    Temperror,
    Permerror
}

public partial class MessageStore : IMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;

    public MessageStore(ILogger<MessageStore> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            _logger.LogInformation("Received message, saving raw data...");
            var raw = buffer.ToArray<byte>();

            var newRawMsg = new RawMessage
            {
                Content = raw
            };

            _dataContext.RawMessages.Add(newRawMsg);
            _dataContext.SaveChanges();
            _logger.LogDebug($"Saved raw message id #{newRawMsg.Id}.");

            _logger.LogInformation("Parsing message & saving data...");
            using var memory = new MemoryStream(raw);
            using var mimeMessage = MimeMessage.Load(memory) ?? throw new Exception("Could not parse message.");

            var isSpam = false;

            try
            {
                isSpam = isSpam && VerifyDkim(mimeMessage);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
            }

            try
            {
                var ip = "goddamn";
                var domain = transaction.From.Host;
                var sender = transaction.From.AsAddress();
                var spfResult = VerifySpf(ip, domain, sender);
                switch (spfResult)
                {
                    case SpfVerifyResult.Pass:
                    case SpfVerifyResult.Softfail:
                    case SpfVerifyResult.Neutral:
                        isSpam = isSpam && false;
                        break;
                    default:
                        isSpam = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
            }

            var newMessage = new Message(mimeMessage)
            {
                RawMessageId = newRawMsg.Id
            };

            _dataContext.Messages.Add(newMessage);
            _dataContext.SaveChanges();

            _logger.LogDebug($"Saved message id #{newMessage.Id}.");

            return Task.FromResult(SmtpResponse.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not save incoming message: {0}", ex.Message);
            return Task.FromResult(SmtpResponse.TransactionFailed);
        }
    }

    private static bool VerifyDkim(MimeMessage mimeMessage)
    {
        var dkimHeaderIndex = mimeMessage.Headers.IndexOf(HeaderId.DkimSignature);
        var hasDkimSignature = dkimHeaderIndex > 0;
        if (hasDkimSignature)
        {
            var locator = new BasicPublicKeyLocator();
            var verifier = new DkimVerifier(locator);
            var dkim = mimeMessage.Headers[dkimHeaderIndex];
            var isDkimSignValid = verifier.Verify(mimeMessage, dkim);
            return isDkimSignValid;
        }

        return true; // no DKIM means it is spam
    }

    public static SpfVerifyResult VerifySpf(string ip, string domain, string sender)
    {
        // https://datatracker.ietf.org/doc/html/rfc7208#section-4.3
        if (domain.Length == 0 || domain.Length > 63 || domain.EndsWith('.'))
        {
            return SpfVerifyResult.None;
        }

        var lookup = new LookupClient();

        // https://datatracker.ietf.org/doc/html/rfc7208#section-4.4
        var rootDomainQueryResult = lookup.Query(domain, QueryType.TXT);
        if (rootDomainQueryResult.HasError)
        {
            return SpfVerifyResult.Temperror;
        }

        // https://datatracker.ietf.org/doc/html/rfc7208#section-3.2
        // https://datatracker.ietf.org/doc/html/rfc7208#section-3.3
        // https://datatracker.ietf.org/doc/html/rfc7208#section-3.4
        // https://datatracker.ietf.org/doc/html/rfc7208#section-4.5
        var rawSpfRecord = rootDomainQueryResult.Answers
            .Select(anws => anws as TxtRecord ?? throw new Exception("Answer was not a TXT Record..."))
            .Single(txtRec => txtRec.Text.Any(t => t.StartsWith("v=spf1 ")));  //
        var spfRecord = string.Concat(rawSpfRecord.Text);

        // https://datatracker.ietf.org/doc/html/rfc7208#section-3.4
        var isTooBig = System.Text.Encoding.ASCII.GetBytes(spfRecord).Length > 512;
        if (isTooBig)
        {
            return SpfVerifyResult.Permerror;
        }

        // https://datatracker.ietf.org/doc/html/rfc7208#section-6
        var redirectExpModInvalid = RedirectModRegex().Count(spfRecord) > 1
            || ExpModRegex().Count(spfRecord) > 1;
        if (redirectExpModInvalid)
        {
            return SpfVerifyResult.Permerror;
        }

        // https://datatracker.ietf.org/doc/html/rfc7208#section-4.6.1
        var spfParts = spfRecord
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToList();

        var spfVersionPart = spfParts.Single(sp => sp == "v=spf1");
        spfParts.Remove(spfVersionPart);

        var defaultResult = SpfVerifyResult.Pass; // uh? checker le RFC

        var allMechanism = spfParts.SingleOrDefault(sp => sp.EndsWith("all"));
        if (allMechanism != null)
        {
            spfParts.Remove(allMechanism);
            defaultResult = allMechanism[0] switch
            {
                '+' => SpfVerifyResult.Pass,
                '-' => SpfVerifyResult.Fail,
                '~' => SpfVerifyResult.Softfail,
                '?' => SpfVerifyResult.Neutral,
                _ => SpfVerifyResult.Pass // no qualifier means +/pass
            };
        }

        foreach (var spfPart in spfParts)
        {
            var isModifier = spfPart.Contains('=');
            if (isModifier)
            {
                // redirect, exp
                return ProcessModifier(ip, sender, spfPart);
            }

            //mech: include, a,mx,ip4,ip6,exists
            var mechanism = spfPart.Split(':')[0];
            var value = spfPart.Split(':')[1];
            switch (mechanism)
            {
                case "include":
                    return VerifySpf(ip, value, sender);
                case "ip4":
                    // if(ip contained_in(ip_range)) {return SpfVerifyResult.Pass} break; 
                    throw new NotImplementedException("exp modifier is not implemented...");
                default:
                    return SpfVerifyResult.Permerror;
            }
        }

        return defaultResult;
    }

    private static SpfVerifyResult ProcessModifier(string ip, string sender, string? spfPart)
    {
        var modifier = spfPart.Split('=')[0];
        var value = spfPart.Split('=')[1];
        switch (modifier)
        {
            case "redirect":
                return VerifySpf(ip, value, sender);
            case "exp":
                throw new NotImplementedException("exp modifier is not implemented...");
            default:
                return SpfVerifyResult.Permerror;
        }
    }

    [GeneratedRegex("redirect=")]
    private static partial Regex RedirectModRegex();
    [GeneratedRegex("exp=")]
    private static partial Regex ExpModRegex();
}

