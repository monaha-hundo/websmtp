using Microsoft.AspNetCore.Mvc;
using OtpNet;
using QRCoder;
using websmtp.Database;

namespace websmtp;

public static partial class MessagesEndpoints
{
    public static IResult GetMessage(
    [FromRoute] Guid msgId,
    [FromServices] IReadableMessageStore messages,
    [FromServices] IConfiguration config
    )
    {
        var message = messages.Single(msgId) ?? throw new Exception("Could not find message");
        var displayHtml = config.GetValue<bool>("Security:EnableHtmlDisplay");
        if (!string.IsNullOrWhiteSpace(message.HtmlContent))
        {
            var contentBytes = Convert.FromBase64String(message.HtmlContent);
            var html = System.Text.Encoding.Default.GetString(contentBytes);
            var mimeType = displayHtml ? "text/html" : "text/plain";
            return Results.Content(html, mimeType);
        }
        if (!string.IsNullOrWhiteSpace(message.TextContent))
        {
            var mimeType = "text";
            return Results.Content(message.TextContent, mimeType);
        }
        throw new Exception("Message had neither HtmlContent or TextContent.");
    }

    public static IResult GetMessageAttachement(
        [FromRoute] Guid msgId,
        [FromRoute] string filename,
        [FromServices] IReadableMessageStore messages
    )
    {
        var message = messages.Single(msgId);
        var attachement = message.Attachements.Single(a => a.Filename == filename);
        var contentBytes = Convert.FromBase64String(attachement.Content);
        var mimeType = attachement.MimeType;
        return Results.File(contentBytes, mimeType, filename);
    }

    public static IResult MarkAsRead(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.MarkAsRead(msgIds);
        return Results.Ok();
    }

    public static IResult MarkAsUnread(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.MarkAsUnread(msgIds);
        return Results.Ok();
    }

    public static IResult Delete(
        [FromServices] IReadableMessageStore messages,
        [FromBody] List<Guid> msgIds
    )
    {
        messages.Delete(msgIds);
        return Results.Ok();
    }

    public static IResult Undelete(
        [FromServices] IReadableMessageStore messages,
        [FromBody] List<Guid> msgIds
    )
    {
        messages.Undelete(msgIds);
        return Results.Ok();
    }

    public static IResult Star(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Star(msgIds);
        return Results.Ok();
    }
    public static IResult Unstar(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Unstar(msgIds);
        return Results.Ok();
    }

    public static IResult Spam(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Spam(msgIds);
        return Results.Ok();
    }
    public static IResult NotSpam(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.NotSpam(msgIds);
        return Results.Ok();
    }

    public static IResult OtpInitiate(
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);

        byte[] raw = new byte[10];
        Random.Shared.NextBytes(raw);
        var otpSecret = Base32Encoding.ToString(raw);

        user.OtpSecret = otpSecret;
        user.OtpEnabled = false; // prevent lock out

        var qrGenerator = new QRCodeGenerator();
        var totpQrCodeString = new OtpUri(OtpType.Totp, user.OtpSecret, user.Username).ToString();
        var qrCodeData = qrGenerator.CreateQrCode(totpQrCodeString, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);

        var bytes = qrCode.GetGraphic(4, true);

        data.SaveChanges();

        return Results.Bytes(bytes, "image/png");
    }

    public static IResult OtpValidateAndEnable(
        [FromBody] OtpValidateViewModel otpData,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);
        var secretBytes = Base32Encoding.ToBytes(user.OtpSecret);
        var totp = new Totp(secretBytes);
        var result = totp.VerifyTotp(otpData.Otp, out var timeSteps);

        if (result)
        {
            user.OtpEnabled = true;
            data.SaveChanges();
        }

        return result
            ? Results.Ok()
            : Results.BadRequest("invalid otp");
    }

    public static IResult ChangePassword(
        [FromBody] ChangePasswordViewModel pwdData,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);

        var passwordHasher = new PasswordHasher();
        var canChange = pwdData.NewPassword == pwdData.ConfirmPassword
            && passwordHasher.VerifyHashedPassword(user.PasswordHash, pwdData.CurrentPassword);

        if (!canChange)
        {
            return Results.BadRequest();
        }

        user.PasswordHash = passwordHasher.HashPassword(pwdData.NewPassword);
        data.SaveChanges();

        return Results.Ok();
    }

}
