using DNS.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MimeKit.Cryptography;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;



//using System.Net.Mail;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.services;

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

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "TEST");

        client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task LoginToInbox()
    {
        try
        {
            DeleteTesterUser();
            await LoginAsTester();

            var testResponse = await client.GetAsync("https://localhost:1443/inbox");

            testResponse.EnsureSuccessStatusCode();

            var content = await testResponse.Content.ReadAsStringAsync();

            Assert.IsTrue(content.Contains("Inbox"));

        }
        finally
        {
            DeleteTesterUser();
        }
    }

    private async Task LoginAsTester()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var rnd = new byte[40];
        Random.Shared.NextBytes(rnd);
        var passwordToHash = System.Text.Encoding.UTF8.GetString(rnd);
        var hasher = new PasswordHasher();
        var passwordHash = hasher.HashPassword(passwordToHash);

        var testerUser = new User
        {
            Username = "tester",
            PasswordHash = passwordHash,
            OtpSecret = string.Empty,
            OtpEnabled = false,
            Deleted = false,
            Mailboxes = [new UserMailbox{
                DisplayName = "Tester @ websmtp.local",
                Host = "websmtp.local",
                Identity = "tester"
            }],
            Identities = [new UserIdentity{
                DisplayName = "Tester",
                Email = "tester@websmtp.local"
            }]
        };

        db.Users.Add(testerUser);
        db.SaveChanges();

        var response = client.GetAsync("https://localhost:1443/login").Result;
        var verificationToken = await response.Content.ReadAsStringAsync();
        if (verificationToken != null && verificationToken.Length > 0)
        {
            verificationToken = verificationToken.Substring(verificationToken.IndexOf("__RequestVerificationToken"));
            verificationToken = verificationToken.Substring(verificationToken.IndexOf("value=\"") + 7);
            verificationToken = verificationToken.Substring(0, verificationToken.IndexOf("\""));
        }
        var contentToSend = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "tester"),
            new KeyValuePair<string, string>("password", passwordToHash),
            new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
        });

        await client.PostAsync("https://localhost:1443/login", contentToSend);
    }

    private void DeleteTesterUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var testerUsers = db.Users
            .Include(u => u.Mailboxes)
            .Where(u => u.Username == "tester")
            .ToList();

        foreach (var testerUser in testerUsers)
        {
            var testerMsg = db.Messages.Include(m => m.RawMessage).Include(m => m.Attachements).Where(m => m.UserId == testerUser.Id).ToList();
            var rawMessage = testerMsg.Select(m => m.RawMessage).ToList();
            var attachements = testerMsg.SelectMany(m => m.Attachements).ToList();

            db.MessageAttachements.RemoveRange(attachements);
            db.SaveChanges();
            db.Messages.RemoveRange(testerMsg);
            db.SaveChanges();
            db.RawMessages.RemoveRange(rawMessage);
            db.SaveChanges();
            db.Mailboxes.RemoveRange(testerUser.Mailboxes);
            db.SaveChanges();
            db.Users.Remove(testerUser);
            db.SaveChanges();
        }

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

        emailAddrs.Add(new MailAddress("tester@websmtp.local", "Tester"));
        
        // for (int e = 0; e < 15; e++)
        // {
        //     emailAddrs.Add(new MailAddress($"user.{e}@websmtp.local", $"User {e}"));
        // }

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

            Assert.IsTrue(savedMessageCount == testEmailCount, "Did not find the expected number of saved emails.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed, exception while sending emails:");
            Console.WriteLine(ex);
            throw;
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

        emailAddrs.Add(new MailAddress("tester@websmtp.local", "Tester"));

        // for (int e = 0; e < 15; e++)
        // {
        //     emailAddrs.Add(new MailAddress($"user.{e}@websmtp.local", $"User {e}"));
        // }

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

        DeleteTesterUser();
        LoginAsTester().Wait();

        try
        {
            Console.WriteLine($"Sending {testEmailCount} emails...");

            mimeMessages.ForEach(sendMailService.SendMail);

            var savedMessageCount = db.Messages.Count(msg => msg.Subject.Contains(testRunId));
            //var filterResult = msgStore.Latest(1, 1000, false, false, true, false, false, msgGuidToFind);

            var testResponse = client.GetAsync($"https://localhost:1443/inbox?filter={msgGuidToFind}").Result;

            testResponse.EnsureSuccessStatusCode();

            var content = testResponse.Content.ReadAsStringAsync().Result;

            Assert.IsTrue(content.Contains(msgGuidToFind));
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
            DeleteTesterUser();
            files?.ForEach(f => f.Content?.Dispose());
            mimeMessages?.ForEach(m => m?.Dispose());
            messages?.ForEach(m => m?.Dispose());
            tokenSource2.Cancel();
        }
    }
}