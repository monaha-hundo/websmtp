using System.Diagnostics;
using System.Net.Mail;
using Bogus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace tests;

[TestClass]
public class UnitTest1
{
    public TestContext TestContext { get; set; }

    private static TestContext _testContext;

    [ClassInitialize]
    public static void SetupTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    private readonly WebApplicationFactory<Program> _factory;

    public UnitTest1()
    {
        _factory = new WebApplicationFactory<Program>();
    }
    [TestMethod]
    public void Send_A_Hundred_Emails()
    {
        // test data generation
        Console.WriteLine("Generating test data...");
        var testEmailCount = 100;
        var messages = new Faker<MailMessage>()
            .CustomInstantiator(f => new MailMessage(f.Internet.Email(), f.Internet.Email()))
            .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
            .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence())
            .Generate(testEmailCount);

        // Execution
        Console.WriteLine($"Sending {testEmailCount} emails...");
        var smtpClient = new SmtpClient("localhost", 1025);
        try
        {
            messages.ForEach(m => smtpClient.Send(m));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed, exception while sending emails:");
            Console.WriteLine(ex);
        }
    }

    [TestMethod]
    public void Send_And_Receive_A_Thousand_Emails()
    {
        // setup
        Console.WriteLine("Seting up tests app/client...");
        var client = _factory.CreateClient();
        var basicOnlineTest = client.GetAsync("/").Result;
        Assert.IsTrue(basicOnlineTest.IsSuccessStatusCode, "Could not contact the webapp.");

        // test data generation
        Console.WriteLine("Generating test data...");
        var testEmailCount = 1000;

        var emailAddress = new Faker<string>()
            .CustomInstantiator(f=> f.Internet.Email())
            .Generate(10);

        var messages = new Faker<MailMessage>()
            .CustomInstantiator(f => new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress)))
            .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
            .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence(10,6))
            .Generate(testEmailCount);

        // Execution
        Console.WriteLine($"Sending {testEmailCount} emails...");
        var smtpClient = new SmtpClient("localhost", 1025);
        try
        {
            messages.ForEach(m => smtpClient.Send(m));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed, exception while sending emails:");
            Console.WriteLine(ex);
            Assert.Fail("Test failed, exception while sending emails.");
        }
        // Validation
        Console.WriteLine("Validating received emails count matches the sent count...");
        var messageStore = _factory.Services.GetService(typeof(IReadableMessageStore)) as IReadableMessageStore
            ?? throw new Exception("Could not get IReadableMessageStore from the app's services");
        var receivedMessageCount = messageStore.Count(onlyNew: false);

        Console.WriteLine($"Found {receivedMessageCount} in message store...");
        Assert.IsTrue(receivedMessageCount == messages.Count);
    }
}