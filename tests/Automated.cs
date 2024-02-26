using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using System.Net.Mail;
using websmtp.Database;

namespace Tests;

[TestClass]
public class Basic
{
    public TestContext? TestContext { get; set; }

    private static TestContext? _testContext;

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
    public void SendEmail()
    {
        using var client = _factory.CreateDefaultClient();
        using var scope = _factory.Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        Console.WriteLine("Generating test data...");
        var testEmailCount = 100;

        var emailAddress = new Faker<MailAddress>()
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
                var message = new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress));

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    var file = f.PickRandom(files);
                    message.Attachments.Add(new Attachment(file.Content, file.FileName));
                }

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    message.CC.Add(f.PickRandom(emailAddress));
                }

                for (int i = 0; i < f.Random.Number(0, 10); i++)
                {
                    message.Bcc.Add(f.PickRandom(emailAddress));
                }

                return message;
            })
            .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
            .RuleFor(m => m.Subject, (f) => $"Test run_id={testRunId} msg_id={Guid.NewGuid()}")
            .RuleFor(m => m.Priority, (f) => f.Random.Enum<MailPriority>())
            .Generate(testEmailCount);

        try
        {
            Console.WriteLine($"Sending {testEmailCount} emails...");
            var smtpClient = new SmtpClient("127.0.0.1", 1025);
            messages.ForEach(m => smtpClient.Send(m));

            var savedMessageCount = db.Messages.Count(msg => msg.Subject.Contains(testRunId));

            Assert.IsTrue(savedMessageCount == testEmailCount, "Did not find the expected number of saved emails...");
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
        }
    }
    // [TestMethod]
    // public void Send_A_Hundred_Emails()
    // {

    //     // test data generation
    //     Console.WriteLine("Generating test data...");
    //     var testEmailCount = 10;

    //     var emailAddress = new Faker<string>()
    //         .CustomInstantiator(f => f.Internet.Email())
    //         .Generate(10);

    //     var files = new Faker<FakeFile>()
    //         .RuleFor(ff => ff.FileName, f => f.Lorem.Slug() + "." + f.Lorem.Word())
    //         .RuleFor(ff => ff.Content, f => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(f.Lorem.Paragraphs(10))))
    //         .Generate(10);

    //     try
    //     {
    //         var messages = new Faker<MailMessage>()
    //             .CustomInstantiator(f =>
    //             {
    //                 var message = new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress));
    //                 var toInsert = f.PickRandom(files, f.Random.Number(6));
    //                 var atts = toInsert.Select(f => new Attachment(f.Content, f.FileName)).ToList();
    //                 atts.ForEach(a => message.Attachments.Add(a));
    //                 return message;
    //             })
    //             .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
    //             .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence(10, 6))
    //             .Generate(testEmailCount);

    //         // Execution
    //         Console.WriteLine($"Sending {testEmailCount} emails...");
    //         var smtpClient = new SmtpClient("localhost", 1025);
    //         messages.ForEach(m => smtpClient.Send(m));
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine("Test failed, exception while sending emails:");
    //         Console.WriteLine(ex);
    //         throw;
    //         //Assert.Fail("Test failed, exception while sending emails.");
    //     }
    //     finally
    //     {
    //         files?.ForEach(f => f.Content?.Dispose());
    //     }
    // }


    // [TestMethod]
    // public void Send_And_Receive_A_Thousand_Emails()
    // {
    //     // setup
    //     Console.WriteLine("Seting up tests app/client...");
    //     var client = _factory.CreateClient();
    //     var basicOnlineTest = client.GetAsync("/").Result;
    //     Assert.IsTrue(basicOnlineTest.IsSuccessStatusCode, "Could not contact the webapp.");

    //     // test data generation
    //     Console.WriteLine("Generating test data...");
    //     var testEmailCount = 1000;

    //     var emailAddress = new Faker<string>()
    //         .CustomInstantiator(f => f.Internet.Email())
    //         .Generate(10);

    //     var files = new Faker<FakeFile>()
    //         .RuleFor(ff => ff.FileName, f => f.Lorem.Slug() + "." + f.Lorem.Word())
    //         .RuleFor(ff => ff.Content, f => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(f.Lorem.Paragraphs(10))))
    //         .Generate(10);

    //     try
    //     {
    //         var messages = new Faker<MailMessage>()
    //             .CustomInstantiator(f =>
    //             {
    //                 var message = new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress));
    //                 var file = f.PickRandom(files);
    //                 var att = new Attachment(file.Content, file.FileName);
    //                 message.Attachments.Add(att);
    //                 return message;
    //             })
    //             .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
    //             .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence(10, 6))
    //             .Generate(testEmailCount);

    //         // Execution
    //         Console.WriteLine($"Sending {testEmailCount} emails...");
    //         var smtpClient = new SmtpClient("localhost", 1025);
    //         messages.ForEach(m => smtpClient.Send(m));

    //         // Validation
    //         Console.WriteLine("Validating received emails count matches the sent count...");
    //         var messageStore = _factory.Services.GetService(typeof(IReadableMessageStore)) as IReadableMessageStore
    //             ?? throw new Exception("Could not get IReadableMessageStore from the app's services");
    //         var receivedMessageCount = messageStore.Count(onlyNew: false);

    //         Console.WriteLine($"Found {receivedMessageCount} in message store...");
    //         Assert.IsTrue(receivedMessageCount == messages.Count);
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine("Test failed, exception while sending emails:");
    //         Console.WriteLine(ex);
    //         throw;
    //         //Assert.Fail("Test failed, exception while sending emails.");
    //     }
    //     finally
    //     {
    //         files?.ForEach(f => f.Content?.Dispose());
    //     }
    // }
}