using DnsClient;
using DnsClient.Protocol;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;
using System.Net;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace websmtp;

public class BasicPublicKeyLocator : DkimPublicKeyLocatorBase
{
    private readonly Dictionary<string, AsymmetricKeyParameter> cache;
    private readonly LookupClient lookupClient;
    private readonly IConfiguration _config;

    private string DnsServer { get; set; }
    private int DnsPort { get; set; }

    public BasicPublicKeyLocator(IConfiguration config)
    {
        _config = config;

        DnsServer = _config?.GetValue<string>("DNS:IP") ?? throw new Exception("Missing DNS:IP config key.");
        DnsPort = _config?.GetValue<int>("DNS:PORT") ?? throw new Exception("Missing DNS:Port config key.");
        cache = [];

        var ipEndpoint = new IPEndPoint(IPAddress.Parse(DnsServer), DnsPort);
        lookupClient = new LookupClient(ipEndpoint);
    }

    private AsymmetricKeyParameter DnsLookup(string domain, string selector, CancellationToken cancellationToken)
    {
        var query = selector + "._domainkey." + domain;

        // checked if we've already fetched this key
        if (cache.TryGetValue(query, out var cachedPubkey))
        {
            return cachedPubkey;
        }

        var response = lookupClient.QueryAsync(query, QueryType.TXT, cancellationToken: cancellationToken).Result;

        var records = response.Answers
            .Select(anws => anws as TxtRecord ?? throw new Exception("Answer was not a TXT Record..."))
            .SelectMany(txtRec => txtRec.Text)
            .ToList();  //

        var record = string.Concat(records);

        var pubkey = GetPublicKey(record);

        cache.Add(query, pubkey);

        return pubkey;
    }

    public override AsymmetricKeyParameter LocatePublicKey(string methods, string domain, string selector, CancellationToken cancellationToken = default)
    {
        var methodList = methods.Split(':', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < methodList.Length; i++)
        {
            if (methodList[i] == "dns/txt")
            {
                return DnsLookup(domain, selector, cancellationToken);
            }
        }

        throw new NotSupportedException($"{methods} does not include any suported lookup methods.");
    }

    public override Task<AsymmetricKeyParameter> LocatePublicKeyAsync(string methods, string domain, string selector, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            return LocatePublicKey(methods, domain, selector, cancellationToken);
        }, cancellationToken);
    }
}