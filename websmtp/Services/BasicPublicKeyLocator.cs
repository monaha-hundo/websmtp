using System.Text;
using Heijden.DNS;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;

namespace websmtp;

public class BasicPublicKeyLocator : DkimPublicKeyLocatorBase
{
    readonly Dictionary<string, AsymmetricKeyParameter> cache;
    readonly Resolver resolver;

    public BasicPublicKeyLocator()
    {
        cache = new Dictionary<string, AsymmetricKeyParameter>();

        resolver = new Resolver()//"8.8.8.8"
        {
            TransportType = TransportType.Udp,
            UseCache = true,
            Retries = 3
        };
    }

    AsymmetricKeyParameter DnsLookup(string domain, string selector, CancellationToken cancellationToken)
    {
        var query = selector + "._domainkey." + domain;
        AsymmetricKeyParameter pubkey;

        // checked if we've already fetched this key
        if (cache.TryGetValue(query, out pubkey))
            return pubkey;

        // make a DNS query
        var response = resolver.Query(query, QType.TXT);
        var builder = new StringBuilder();

        // combine the TXT records into 1 string buffer
        foreach (var record in response.RecordsTXT)
        {
            foreach (var text in record.TXT)
                builder.Append(text);
        }

        var txt = builder.ToString();

        // DkimPublicKeyLocatorBase provides us with this helpful method.
        pubkey = GetPublicKey(txt);

        cache.Add(query, pubkey);

        return pubkey;
    }

    public override AsymmetricKeyParameter LocatePublicKey(string methods, string domain, string selector, CancellationToken cancellationToken = default)
    {
        var methodList = methods.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < methodList.Length; i++)
        {
            if (methodList[i] == "dns/txt")
                return DnsLookup(domain, selector, cancellationToken);
        }

        throw new NotSupportedException(string.Format("{0} does not include any suported lookup methods.", methods));
    }

    public override Task<AsymmetricKeyParameter> LocatePublicKeyAsync(string methods, string domain, string selector, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            return LocatePublicKey(methods, domain, selector, cancellationToken);
        }, cancellationToken);
    }
}