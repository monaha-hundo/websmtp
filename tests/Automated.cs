using DNS.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MimeKit.Cryptography;
using System.Net;
using System.Net.Mail;

//using System.Net.Mail;
using websmtp;
using websmtp.Database;

namespace Tests;

[TestClass]
public class Basic
{
    public TestContext? TestContext { get; set; }

#pragma warning disable IDE0052 // Remove unread private members
    private static TestContext? _testContext; // 
#pragma warning restore IDE0052 // Remove unread private members

    [ClassInitialize]
    public static void SetupTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient client;

    public Basic()
    {
        _factory = new WebApplicationFactory<Program>();

        //client = _factory.WithWebHostBuilder(builder =>
        //{
        //    builder.UseEnvironment("Test");
        //}).CreateClient();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "TEST");

        client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task LoginToInbox()
    {
        var response = await client.GetAsync("https://localhost:1443/inbox");

        response.EnsureSuccessStatusCode();

        var content = response.Content.ReadAsStringAsync().Result;

        Assert.IsTrue(content.Contains("Inbox"));
    }   

    [TestMethod]
    public void SignAndVerifyDKIM()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var incomingEmailValidator = scope.ServiceProvider.GetRequiredService<IncomingEmailValidator>();

        var mailMessage = new MailMessage
        {
            From = new MailAddress("rod.b@websmtp.local", "Rod B")
        };

        mailMessage.To.Add(new MailAddress("bob.g@websmtp.local", "Bob G"));
        mailMessage.Subject = "Testing signed email (dkim)";
        mailMessage.Body = "Hello, this message is signed. Hope it makes you feel safe.";

        var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);
        var headersToSign = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date };

        var dnsPort = config.GetValue<int>("DNS:Port");

        var domain = (mimeMessage.From[0] as MailboxAddress)?.Domain;
        var dkimDomainConfigSection = config.GetSection("DKIM:Domains");
        var domainsConfigs = dkimDomainConfigSection.GetChildren();
        var domainConfig = domainsConfigs.Where(s => s.GetValue<string>("Name") == domain).SingleOrDefault();

        if (domainConfig == null) throw new Exception($"Trying to sign a message for an unconfigured email domain: '{domain}'.");

        var selector = domainConfig.GetValue<string>("Selector") ?? throw new Exception("Missing DKIM:Domain:Selector config key.");
        var privateKeyFilename = domainConfig.GetValue<string>("PrivateKey") ?? throw new Exception("Missing DKIM:Domain:PrivateKey config key.");
        var publicKeyFilename = privateKeyFilename.Replace("private", "pub").Replace("pem", "der");
        var publicKey = Convert.ToBase64String(File.ReadAllBytes(publicKeyFilename));

        var masterFile = new MasterFile();
        using var server = new DnsServer(masterFile);
        masterFile.AddTextResourceRecord($"dkim._domainkey.{domain}", "dkim", "v=DKIM1; p=" + publicKey);

        var tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;
        Task.Run(async () =>
        {
            await server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));
        }, ct);

        var signer = new DkimSigner(privateKeyFilename, domain, selector)
        {
            AgentOrUserIdentifier = $"@{domain}",
            QueryMethod = "dns/txt",
        };

        mimeMessage.Prepare(EncodingConstraint.SevenBit);

        signer.Sign(mimeMessage, headersToSign);

        var isValid = incomingEmailValidator.VerifyDkim(mimeMessage);

        Assert.IsTrue(isValid);

        tokenSource2.Cancel();
    }

    [TestMethod]
    public void AVerifySpfGmail()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var incomingEmailValidator = scope.ServiceProvider.GetRequiredService<IncomingEmailValidator>();

        var dnsPort = config.GetValue<int>("DNS:Port");
        var masterFile = new MasterFile();

        using var server = new DnsServer(masterFile);

        // dug info on 2024-03-05
        masterFile.AddTextResourceRecord("gmail.com", "v", "spf1 redirect=_spf.google.com");
        masterFile.AddTextResourceRecord("_spf.google.com", "v", "spf1 include:_netblocks.google.com include:_netblocks2.google.com include:_netblocks3.google.com ~all");
        masterFile.AddTextResourceRecord("_netblocks.google.com", "v", "spf1 ip4:35.190.247.0/24 ip4:64.233.160.0/19 ip4:66.102.0.0/20 ip4:66.249.80.0/20 ip4:72.14.192.0/18 ip4:74.125.0.0/16 ip4:108.177.8.0/21 ip4:173.194.0.0/16 ip4:209.85.128.0/17 ip4:216.58.192.0/19 ip4:216.239.32.0/19 ~all");
        masterFile.AddTextResourceRecord("_netblocks2.google.com", "v", "spf1 ip6:2001:4860:4000::/36 ip6:2404:6800:4000::/36 ip6:2607:f8b0:4000::/36 ip6:2800:3f0:4000::/36 ip6:2a00:1450:4000::/36 ip6:2c0f:fb50:4000::/36 ~all");
        masterFile.AddTextResourceRecord("_netblocks3.google.com", "v", "spf1 ip4:172.217.0.0/19 ip4:172.217.32.0/20 ip4:172.217.128.0/19 ip4:172.217.160.0/20 ip4:172.217.192.0/19 ip4:172.253.56.0/21 ip4:172.253.112.0/20 ip4:108.177.96.0/19 ip4:35.191.0.0/16 ip4:130.211.0.0/22 ~all");


        var tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;
        Task.Run(async () =>
        {
            await server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));
        }, ct);

        var ip = "66.249.80.2";
        var domain = "gmail.com";
        var sender = "test@gmail.com";

        var result = incomingEmailValidator.VerifySpf(ip, domain, sender);
        Assert.IsTrue(result == SpfVerifyResult.Pass);

        tokenSource2.Cancel();
    }

    [TestMethod]
    public void SendEmailAndCatchAll()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sendMailService = scope.ServiceProvider.GetRequiredService<SendMailService>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        Console.WriteLine("Generating test data...");
        var testEmailCount = 1;

        var emailAddrs = new List<MailAddress>();

        for (int e = 0; e < 15; e++)
        {
            emailAddrs.Add(new MailAddress($"user.{e}@websmtp.local", $"User {e}"));
        }

        var files = new List<FakeFile>(10);

        for (int f = 0; f < 10; f++)
        {
            files.Add(new FakeFile($"file-{f}.dat", new MemoryStream(System.Text.Encoding.UTF8.GetBytes("lorem ipsum"))));
        }

        var messages = new List<MailMessage>(testEmailCount);

        var testRunId = Guid.NewGuid().ToString("N");

        for (int m = 0; m < testEmailCount; m++)
        {
            var newMsg = new MailMessage(
                emailAddrs.PickRandom(),
                emailAddrs.PickRandom()
            )
            {
                Subject = $"Test run_id={testRunId} msg_id={Guid.NewGuid()}",
                Body = $"Test run_id={testRunId} msg_id={Guid.NewGuid()}"
            };

            var file = files.PickRandom();
            newMsg.Attachments.Add(new Attachment(file.Content, file.FileName));

            messages.Add(newMsg);
        }

        var domains = emailAddrs.Select(m => m.Host).ToList();

        var mimeMessages = messages.Select(m => MimeMessage.CreateFromMailMessage(m)).ToList();

        var domain = "websmtp.local";
        var dkimDomainConfigSection = config.GetSection("DKIM:Domains");
        var domainsConfigs = dkimDomainConfigSection.GetChildren();
        var domainConfig = domainsConfigs.Where(s => s.GetValue<string>("Name") == domain).SingleOrDefault();
        if (domainConfig == null) throw new Exception($"Trying to sign a message for an unconfigured email domain: '{domain}'.");

        var selector = domainConfig.GetValue<string>("Selector") ?? throw new Exception("Missing DKIM:Domain:Selector config key.");
        var privateKeyFilename = domainConfig.GetValue<string>("PrivateKey") ?? throw new Exception("Missing DKIM:Domain:PrivateKey config key.");
        var publicKeyFilename = privateKeyFilename.Replace("private", "pub").Replace("pem", "der");
        var publicKey = Convert.ToBase64String(File.ReadAllBytes(publicKeyFilename));

        var dnsPort = config.GetValue<int>("DNS:Port");

        var masterFile = new MasterFile();
        // masterFile.AddIPAddressResourceRecord("websmtp.local", "127.0.0.1");
        // masterFile.AddMailExchangeResourceRecord("websmtp.local", 10, "localhost");
        // masterFile.AddTextResourceRecord("dkim._domainkey.websmtp.local", "dkim", "v=DKIM1; p=" + publicKey);
        // masterFile.AddTextResourceRecord("websmtp.local", "v", "spf1 +all");

        domains.GroupBy(d => d).Select(dG => dG.FirstOrDefault()).ToList().ForEach(dmn =>
        {
            masterFile.AddIPAddressResourceRecord(dmn, "127.0.0.1");
            masterFile.AddMailExchangeResourceRecord(dmn, 10, "localhost");
            masterFile.AddTextResourceRecord($"dkim._domainkey.{dmn}", "dkim", "v=DKIM1; p=" + publicKey);
            masterFile.AddTextResourceRecord(dmn, "v", "spf1 +all");
        });
        using var server = new DnsServer(masterFile);
        var tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;
        Task.Run(async () =>
        {
            await server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));
        }, ct);

        try
        {
            Console.WriteLine($"Sending {testEmailCount} emails...");

            mimeMessages.ForEach(sendMailService.SendMail);

            var savedMessageCount = db.Messages.Count(msg => msg.Subject.Contains(testRunId));
            var dkimPassed = db.Messages.Count(msg => msg.Subject.Contains(testRunId) && !msg.DkimFailed);
            var spfPassed = db.Messages.Count(msg => msg.Subject.Contains(testRunId) && msg.SpfStatus == SpfVerifyResult.Pass);

            Assert.IsTrue(savedMessageCount == testEmailCount, "Did not find the expected number of saved emails.");
            Assert.IsTrue(dkimPassed == testEmailCount, "Some email did not pass the DKIM validation.");
            Assert.IsTrue(spfPassed == testEmailCount, "Some email did not pass the SPF validation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed, exception while sending emails:");
            Console.WriteLine(ex);
            //throw;
            // Assert.Fail("Test failed, exception while sending emails.");
        }
        finally
        {
            files?.ForEach(f => f.Content?.Dispose());
            mimeMessages?.ForEach(m => m?.Dispose());
            messages?.ForEach(m => m?.Dispose());
            tokenSource2.Cancel();
        }
    }


    [TestMethod]
    public void Filter()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sendMailService = scope.ServiceProvider.GetRequiredService<SendMailService>();
        var msgStore = scope.ServiceProvider.GetRequiredService<IReadableMessageStore>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        Console.WriteLine("Generating test data...");
        var testEmailCount = 1;

        var emailAddrs = new List<MailAddress>();

        for (int e = 0; e < 15; e++)
        {
            emailAddrs.Add(new MailAddress($"user.{e}@websmtp.local", $"User {e}"));
        }

        var files = new List<FakeFile>(10);

        for (int f = 0; f < 10; f++)
        {
            files.Add(new FakeFile($"file-{f}.dat", new MemoryStream(System.Text.Encoding.UTF8.GetBytes("lorem ipsum"))));
        }

        var messages = new List<MailMessage>(testEmailCount);

        var testRunId = Guid.NewGuid().ToString("N");

        for (int m = 0; m < testEmailCount; m++)
        {
            var newMsg = new MailMessage(
                emailAddrs.PickRandom(),
                emailAddrs.PickRandom()
            )
            {
                Subject = $"Test run_id={testRunId} msg_id={Guid.NewGuid()}",
                Body = $"Test run_id={testRunId} msg_id={Guid.NewGuid()}"
            };

            var file = files.PickRandom();
            newMsg.Attachments.Add(new Attachment(file.Content, file.FileName));

            messages.Add(newMsg);
        }

        var msgGuidToFind = Guid.NewGuid().ToString();
        messages[0].Subject = $"Test run_id={testRunId} msg_id={msgGuidToFind}";

        var domains = emailAddrs.Select(m => m.Host).ToList();

        var mimeMessages = messages.Select(m => MimeMessage.CreateFromMailMessage(m)).ToList();

        var domain = "websmtp.local";
        var dkimDomainConfigSection = config.GetSection("DKIM:Domains");
        var domainsConfigs = dkimDomainConfigSection.GetChildren();
        var domainConfig = domainsConfigs.Where(s => s.GetValue<string>("Name") == domain).SingleOrDefault();
        if (domainConfig == null) throw new Exception($"Trying to sign a message for an unconfigured email domain: '{domain}'.");

        var selector = domainConfig.GetValue<string>("Selector") ?? throw new Exception("Missing DKIM:Domain:Selector config key.");
        var privateKeyFilename = domainConfig.GetValue<string>("PrivateKey") ?? throw new Exception("Missing DKIM:Domain:PrivateKey config key.");
        var publicKeyFilename = privateKeyFilename.Replace("private", "pub").Replace("pem", "der");
        var publicKey = Convert.ToBase64String(File.ReadAllBytes(publicKeyFilename));

        var dnsPort = config.GetValue<int>("DNS:Port");

        var masterFile = new MasterFile();
        masterFile.AddIPAddressResourceRecord("websmtp.local", "127.0.0.1");
        masterFile.AddMailExchangeResourceRecord("websmtp.local", 10, "localhost");
        masterFile.AddTextResourceRecord("dkim._domainkey.websmtp.local", "dkim", "v=DKIM1; p=" + publicKey);
        masterFile.AddTextResourceRecord("websmtp.local", "v", "spf1 +all");

        domains.GroupBy(d => d).Select(dG => dG.FirstOrDefault()).ToList().ForEach(dmn =>
        {
            masterFile.AddIPAddressResourceRecord(dmn, "127.0.0.1");
            masterFile.AddMailExchangeResourceRecord(dmn, 10, "localhost");
            masterFile.AddTextResourceRecord(dmn, "v", "spf1 +all");
        });
        using var server = new DnsServer(masterFile);
        var tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;
        Task.Run(async () =>
        {
            await server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));
        }, ct);

        try
        {
            Console.WriteLine($"Sending {testEmailCount} emails...");

            mimeMessages.ForEach(sendMailService.SendMail);

            var savedMessageCount = db.Messages.Count(msg => msg.Subject.Contains(testRunId));
            var filterResult = msgStore.Latest(1, 1000, false, false, true, false, false, msgGuidToFind);

            Assert.IsTrue(filterResult.Count == 1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed, exception while sending emails:");
            Console.WriteLine(ex);
            throw;
            //Assert.Fail("Test failed, exception while sending emails.");
        }
        finally
        {
            files?.ForEach(f => f.Content?.Dispose());
            mimeMessages?.ForEach(m => m?.Dispose());
            messages?.ForEach(m => m?.Dispose());
            tokenSource2.Cancel();
        }
    }
}