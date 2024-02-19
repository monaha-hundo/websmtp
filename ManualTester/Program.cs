using Bogus;
using ManualTester;
using System.Net.Mail;

Console.WriteLine("Generating test data...");
var testEmailCount = 10;

var emailAddress = new Faker<string>()
    .CustomInstantiator(f => f.Internet.Email())
    .Generate(10);

var files = new Faker<FakeFile>()
    .CustomInstantiator((f) => new FakeFile(
        f.Lorem.Slug() + "." + f.Lorem.Word(),
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(f.Lorem.Paragraphs(10)))
    ))
    .Generate(10);

try
{
    var messages = new Faker<MailMessage>()
        .CustomInstantiator(f =>
        {
            var message = new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress));
            var toInsert = files[0];
            var atts = new List<Attachment>() { new Attachment(toInsert.Content, toInsert.FileName) };
            atts.ForEach(a => message.Attachments.Add(a));
            return message;
        })
        .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
        .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence(10, 6))
        .Generate(testEmailCount);

    // Execution
    Console.WriteLine($"Sending {testEmailCount} emails...");
    var smtpClient = new SmtpClient("127.0.0.1", 1025);
    messages.ForEach(m => smtpClient.Send(m));
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
}