using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using DnsClient;
using DnsClient.Protocol;
using MimeKit;
using MimeKit.Cryptography;

namespace websmtp;

public partial class IncomingEmailValidator
{
    readonly IConfiguration _config;
    readonly BasicPublicKeyLocator _locator;
    private string DnsServer => _config.GetValue<string>("DNS:IP") ?? throw new Exception("Missing DNS config key.");
    private int DnsPort => _config.GetValue<int>("DNS:Port");

    public IncomingEmailValidator(IConfiguration config, BasicPublicKeyLocator locator)
    {
        _config = config;
        _locator = locator;
    }

    public bool VerifyDkim(MimeMessage mimeMessage)
    {
        ArgumentNullException.ThrowIfNull(mimeMessage);

        var fromMailbox = mimeMessage.From[0] as MailboxAddress ?? throw new Exception("Address was not a mailbox address.");
        var fromDomain = fromMailbox.Domain;
        
        if (fromDomain == "localhost")
        {
            return true;
        }

        var dkimHeaderIndex = mimeMessage.Headers.IndexOf(HeaderId.DkimSignature);
        var hasDkimSignature = dkimHeaderIndex > -1;
        if (hasDkimSignature)
        {
            var verifier = new DkimVerifier(_locator);
            var dkim = mimeMessage.Headers[dkimHeaderIndex];
            var isDkimSignValid = verifier.Verify(mimeMessage, dkim);
            return isDkimSignValid;
        }

        return false;
    }

    public SpfVerifyResult VerifySpf(string ip, string domain, string sender)
    {
        if (domain == "localhost")
        {
            return SpfVerifyResult.Pass;
        }

        // https://datatracker.ietf.org/doc/html/rfc7208#section-4.3
        if (domain.Length == 0 || domain.Length > 63 || domain.EndsWith('.'))
        {
            return SpfVerifyResult.None;
        }

        var ipEndpoint = new IPEndPoint(IPAddress.Parse(DnsServer), DnsPort);
        var lookup = new LookupClient(ipEndpoint);

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
                    var isIp4Valid = VerifySpfIp(ip, value);
                    if (isIp4Valid)
                    {
                        return SpfVerifyResult.Pass;
                    }
                    break;
                default:
                    return SpfVerifyResult.Permerror;
            }
        }

        return defaultResult;
    }

    private static bool VerifySpfIp(string ipToValidate, string spfIpMechnismValue)
    {
        var isRange = spfIpMechnismValue.Contains('/');
        if (isRange)
        {
            var ipNet = IPNetwork.Parse(spfIpMechnismValue);
            var isIpInRange = ipNet.Contains(IPAddress.Parse(ipToValidate));
            if (isIpInRange)
            {
                return true;
            }
        }

        var isValidIp = spfIpMechnismValue == ipToValidate;
        if (isValidIp)
        {
            return true;
        }

        return false;
    }

    private SpfVerifyResult ProcessModifier(string ip, string sender, string spfPart)
    {
        var modifier = spfPart.Split('=')[0];
        var value = spfPart.Split('=')[1];
        return modifier switch
        {
            "redirect" => VerifySpf(ip, value, sender),
            "exp" => throw new NotImplementedException("exp modifier is not implemented..."),
            _ => SpfVerifyResult.Permerror,
        };
    }

    [GeneratedRegex("redirect=")]
    private static partial Regex RedirectModRegex();
    [GeneratedRegex("exp=")]
    private static partial Regex ExpModRegex();

}
