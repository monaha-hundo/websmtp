﻿using Bogus;
using ManualTester;
using System.Net.Mail;
using OtpNet;
using QRCoder;

var validator = new SpfValidator()
{
    HeloDomain = DomainName.Parse("example.com"),
    LocalDomain = DomainName.Parse("receivingmta.example.com"),
    LocalIP = IPAddress.Parse("192.0.2.1")
};

SpfQualifier result = validator.CheckHost(IPAddress.Parse("192.0.2.200"), DomainName.Parse("example.com"), "sender@example.com").Result;

return;

// byte[] raw = new byte[10];
// Random.Shared.NextBytes(raw);

// var secret = Base32Encoding.ToString(raw);
// QRCodeGenerator qrGenerator = new QRCodeGenerator();
// var uriString = new OtpUri(OtpType.Totp, secret, "some_user@asd.com").ToString();
// QRCodeData qrCodeData = qrGenerator.CreateQrCode(uriString, QRCodeGenerator.ECCLevel.Q);
// AsciiQRCode qrCode = new AsciiQRCode(qrCodeData);

// var lines = qrCode.GetLineByLineGraphic(1,  drawQuietZones: false);
// foreach (var line in lines)
// {
//     Console.WriteLine(line);
// }

// //var secretBytes = Base32Encoding.ToBytes(secret);
// var totp = new Totp(raw);

// while (true)
// {
//     Console.Write("Enter OTP: ");
//     var input = Console.ReadLine();
//     var result = totp.VerifyTotp(input, out var timeSteps);
//     Console.WriteLine(result);
// }

// return;

// Console.WriteLine("Generating test data...");
// var testEmailCount = 10;

// var emailAddress = new Faker<string>()
//     .CustomInstantiator(f => f.Internet.Email())
//     .Generate(10);

// var files = new Faker<FakeFile>()
//     .CustomInstantiator((f) => new FakeFile(
//         f.Lorem.Slug() + "." + f.Lorem.Word(),
//         new MemoryStream(System.Text.Encoding.UTF8.GetBytes(f.Lorem.Paragraphs(10)))
//     ))
//     .Generate(10);

// try
// {
//     var messages = new Faker<MailMessage>()
//         .CustomInstantiator(f =>
//         {
//             var message = new MailMessage(f.PickRandom(emailAddress), f.PickRandom(emailAddress));
//             var toInsert = files[0];
//             var atts = new List<Attachment>() { new Attachment(toInsert.Content, toInsert.FileName) };
//             atts.ForEach(a => message.Attachments.Add(a));
//             return message;
//         })
//         .RuleFor(m => m.Body, (f) => f.Lorem.Paragraphs(2))
//         .RuleFor(m => m.Subject, (f) => f.Lorem.Sentence(10, 6))
//         .Generate(testEmailCount);

//     // Execution
//     Console.WriteLine($"Sending {testEmailCount} emails...");
//     var smtpClient = new SmtpClient("127.0.0.1", 1025);
//     messages.ForEach(m => smtpClient.Send(m));
// }
// catch (Exception ex)
// {
//     Console.WriteLine("Test failed, exception while sending emails:");
//     Console.WriteLine(ex);
//     throw;
//     //Assert.Fail("Test failed, exception while sending emails.");
// }
// finally
// {
//     files?.ForEach(f => f.Content?.Dispose());
// }