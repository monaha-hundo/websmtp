using DnsClient;
using DnsClient.Protocol;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;

namespace websmtp;

public class BasicPublicKeyLocator : DkimPublicKeyLocatorBase
{
    readonly Dictionary<string, AsymmetricKeyParameter> cache;
    readonly LookupClient lookupClient;

    public BasicPublicKeyLocator()
    {
        cache = new Dictionary<string, AsymmetricKeyParameter>();

        lookupClient = new LookupClient();
    }

    AsymmetricKeyParameter DnsLookup(string domain, string selector, CancellationToken cancellationToken)
    {
        var query = selector + "._domainkey." + domain;

        // checked if we've already fetched this key
        if (cache.TryGetValue(query, out var cachedPubkey))
        {
            return cachedPubkey;
        }

        var response = lookupClient.Query(query, QueryType.TXT);

        var records = response.Answers
            .Select(anws => anws as TxtRecord ?? throw new Exception("Answer was not a TXT Record..."))
            .Select(txtRec => txtRec.Text)
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