using Bogus;
using DNS.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MimeKit.Cryptography;
using System.Net;
using System.Net.Mail;
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

    public Basic()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TestMethod]
    public void SignAndVerifyDKIM()
    {
        using var client = _factory.CreateDefaultClient();
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

        var selector = domainConfig.GetValue<string>("Selector")?? throw new Exception("Missing DKIM:Domain:Selector config key.");
        var privateKeyFilename = domainConfig.GetValue<string>("PrivateKey") ?? throw new Exception("Missing DKIM:Domain:PrivateKey config key.");
        var publicKeyFilename = privateKeyFilename.Replace("private", "pub").Replace("pem", "der");
        var publicKey = Convert.ToBase64String(File.ReadAllBytes(publicKeyFilename));

        var masterFile = new MasterFile();
        var server = new DnsServer(masterFile);
        masterFile.AddTextResourceRecord($"dkim._domainkey.{domain}", "dkim", "v=DKIM1; p=" + publicKey);
        var listenTask = server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));

        var signer = new DkimSigner(privateKeyFilename, domain, selector)
        {
            AgentOrUserIdentifier = $"@{domain}",
            QueryMethod = "dns/txt",
        };

        mimeMessage.Prepare(EncodingConstraint.SevenBit);

        signer.Sign(mimeMessage, headersToSign);

        var isValid = incomingEmailValidator.VerifyDkim(mimeMessage);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifySpfGmail()
    {
        using var client = _factory.CreateDefaultClient();
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var incomingEmailValidator = scope.ServiceProvider.GetRequiredService<IncomingEmailValidator>();

        var dnsPort = config.GetValue<int>("DNS:Port");
        var masterFile = new MasterFile();
        var ipEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 53);
        var server = new DnsServer(masterFile, ipEndpoint);
        var listenTask = server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));

        var ip = "66.249.80.2";
        var domain = "gmail.com";
        var sender = "test@gmail.com";

        var result = incomingEmailValidator.VerifySpf(ip, domain, sender);
        Assert.IsTrue(result == SpfVerifyResult.Pass);
    }

    [TestMethod]
    public void SendEmail()
    {

        using var client = _factory.CreateDefaultClient();
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sendMailService = scope.ServiceProvider.GetRequiredService<SendMailService>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        Console.WriteLine("Generating test data...");
        var testEmailCount = 10;

        var fromEmailAddrs = new Faker<MailAddress>()
            .CustomInstantiator(f =>
            {
                var fn = f.Name.FirstName();
                var ln = f.Name.LastName();
                var prov = "websmtp.local";
                var email = f.Internet.Email(fn, ln, prov, "");
                return new MailAddress(email, $"{fn} {ln}");
            })
            .Generate(15);

        var toEmailAddrs = new Faker<MailAddress>()
            .CustomInstantiator(f =>
            {
                var fn = f.Name.FirstName();
                var ln = f.Name.LastName();
                var prov = f.Internet.DomainName();
                var email = f.Internet.Email(fn, ln, prov, "");
                return new MailAddress(email, $"{fn} {ln}");
            })
            .Generate(15);

        var files = new Faker<FakeFile>()
            .CustomInstantiator((f) => new FakeFile(
                f.Lorem.Slug() + "." + f.Lorem.Word(),
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(f.Lorem.Paragraphs(10)))
            ))
            .Generate(10);

        var testRunId = Guid.NewGuid().ToString("N");

        var messages = new Faker<MailMessage>()
            .CustomInstantiator(f =>
            {
                var message = new MailMessage(f.PickRandom(fromEmailAddrs), f.PickRandom(toEmailAddrs));

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    var file = f.PickRandom(files);
                    message.Attachments.Add(new Attachment(file.Content, file.FileName));
                }

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    message.CC.Add(f.PickRandom(toEmailAddrs));
                }

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    message.Bcc.Add(f.PickRandom(toEmailAddrs));
                }

                return message;
            })
            .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
            .RuleFor(m => m.Subject, (f) => $"Test run_id={testRunId} msg_id={Guid.NewGuid()}")
            .RuleFor(m => m.Priority, (f) => f.Random.Enum<MailPriority>())
            .Generate(testEmailCount);

        var domains = toEmailAddrs.Select(m => m.Host).ToList();

        var mimeMessages = messages.Select(m => MimeMessage.CreateFromMailMessage(m)).ToList();

        var privateKeyFilename = config.GetValue<string>("DKIM:PrivateKey") ?? throw new Exception("Missing DKIM:PrivateKey config key.");
        privateKeyFilename = Path.Join(env.ContentRootPath, privateKeyFilename); // filename relative to the websmtp app, not the tests app
        var publicKeyFilename = privateKeyFilename.Replace("private", "pub").Replace("pem", "der");
        var publicKey = Convert.ToBase64String(File.ReadAllBytes(publicKeyFilename));
        var dnsPort = config.GetValue<int>("DNS:Port");

        var masterFile = new MasterFile();
        masterFile.AddIPAddressResourceRecord("websmtp.local", "127.0.0.1");
        masterFile.AddMailExchangeResourceRecord("websmtp.local", 10, "localhost");
        masterFile.AddTextResourceRecord("dkim._domainkey.websmtp.local", "dkim", "v=DKIM1; p=" + publicKey);
        masterFile.AddTextResourceRecord("websmtp.local", "v", "spf1 +all");

        domains.GroupBy(d=>d).Select(dG=>dG.FirstOrDefault()).ToList().ForEach(dmn => {
            masterFile.AddIPAddressResourceRecord(dmn, "127.0.0.1");
            masterFile.AddMailExchangeResourceRecord(dmn, 10, "localhost");
            masterFile.AddTextResourceRecord(dmn, "v", "spf1 +all");
        });
        var server = new DnsServer(masterFile);
        var listenTask = server.Listen(dnsPort, IPAddress.Parse("127.0.0.1"));

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
            Assert.Fail("Test failed, exception while sending emails.");
        }
        finally
        {
            files?.ForEach(f => f.Content?.Dispose());
            mimeMessages?.ForEach(m => m?.Dispose());
            messages?.ForEach(m => m?.Dispose());
        }
    }
}