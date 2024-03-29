using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using OtpNet;
using QRCoder;
using SmtpServer.Mail;
using websmtp.Database;
using websmtp.Database.Models;

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

    public class TrainRequest
    {
        public List<Guid> MsgsIds { get; set; } = [];
        public bool Spam { get; set; }
    }

    public static async Task<IResult> Train(
        [FromBody] TrainRequest trainRequest,
        [FromServices] IReadableMessageStore messages
    )
    {
        await messages.TrainSpam(trainRequest.MsgsIds, trainRequest.Spam);
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

    public static IResult AddMailbox(
        [FromBody] AddMailboxRequest addMailboxReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var mbAddr = new MimeKit.MailboxAddress(
            addMailboxReq.DisplayName,
            addMailboxReq.Email
        );

        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);

        var newMailbox = new UserMailbox
        {
            DisplayName = addMailboxReq.DisplayName,
            Identity = mbAddr.LocalPart,
            Host = mbAddr.Domain,
        };

        user.Mailboxes.Add(newMailbox);
        data.SaveChanges();

        return Results.Ok();
    }

    public static IResult AddIdentity(
        [FromBody] AddMailboxRequest addMailboxReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var mbAddr = new MimeKit.MailboxAddress(
            addMailboxReq.DisplayName,
            addMailboxReq.Email
        );

        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);

        var newIdentity = new UserIdentity
        {
            DisplayName = addMailboxReq.DisplayName,
            Email = addMailboxReq.Email
        };

        user.Identities.Add(newIdentity);
        data.SaveChanges();

        return Results.Ok();
    }

    public class ChangeUserPasswordRequest
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public static IResult ChangeUserPassword(
        [FromBody] ChangeUserPasswordRequest changePwdReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var user = data.Users.Single(u => u.Id == changePwdReq.UserId);
        var passwordHasher = new PasswordHasher();
        var canChange = changePwdReq.NewPassword == changePwdReq.ConfirmPassword;
        if (!canChange)
        {
            return Results.BadRequest();
        }
        user.PasswordHash = passwordHasher.HashPassword(changePwdReq.NewPassword);
        data.SaveChanges();
        return Results.Ok();
    }

    public class ChangeUsernameRequest
    {
        public int UserId { get; set; }
        public string NewUsername { get; set; } = string.Empty;
    }

    public static IResult ChangeUsername(
        [FromBody] ChangeUsernameRequest changeUsernameReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var user = data.Users.Single(u => u.Id == changeUsernameReq.UserId);
        user.Username = changeUsernameReq.NewUsername;
        data.SaveChanges();
        return Results.Ok();
    }

    public class AddUserMailboxRequest
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public static IResult AddUserMailbox(
        [FromBody] AddUserMailboxRequest addMailboxReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var mbAddr = new MimeKit.MailboxAddress(
            addMailboxReq.DisplayName,
            addMailboxReq.Email
        );

        var user = data.Users.Single(u => u.Id == addMailboxReq.UserId);

        var newMailbox = new UserMailbox
        {
            DisplayName = addMailboxReq.DisplayName,
            Identity = mbAddr.LocalPart,
            Host = mbAddr.Domain,
        };

        user.Mailboxes.Add(newMailbox);
        data.SaveChanges();

        return Results.Ok();
    }

    public static IResult AddUserIdentity(
        [FromBody] AddUserMailboxRequest addUserIdentityReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var mbAddr = new MimeKit.MailboxAddress(
            addUserIdentityReq.DisplayName,
            addUserIdentityReq.Email
        );

        var userId = httpContextAccessor.GetUserId();
        var user = data.Users.Single(u => u.Id == userId);

        var newIdentity = new UserIdentity
        {
            DisplayName = addUserIdentityReq.DisplayName,
            Email = addUserIdentityReq.Email
        };

        user.Identities.Add(newIdentity);
        data.SaveChanges();

        return Results.Ok();
    }

    public class RemoveUserMailboxRequest
    {
        public int UserId { get; set; }
        public int MailboxId { get; set; }
    }

    public static IResult RemoveUserMailbox(
        [FromBody] RemoveUserMailboxRequest removeMailboxReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var user = data.Users
            .Include(u => u.Mailboxes)
            .Single(u => u.Id == removeMailboxReq.UserId);

        var mbToRemove = user.Mailboxes.Single(m => m.Id == removeMailboxReq.MailboxId);

        user.Mailboxes.Remove(mbToRemove);
        data.SaveChanges();

        return Results.Ok();
    }

    public static IResult RemoveUserIdentity(
        [FromBody] RemoveUserMailboxRequest removeIDentityReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var user = data.Users
            .Include(u => u.Identities)
            .Single(u => u.Id == removeIDentityReq.UserId);

        var idToRemove = user.Identities.Single(m => m.Id == removeIDentityReq.MailboxId);

        user.Identities.Remove(idToRemove);
        data.SaveChanges();

        return Results.Ok();
    }

    public class AddUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public static IResult AddUser(
        [FromBody] AddUserRequest addUserReq,
        [FromServices] DataContext data
    )
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(addUserReq.Password);

        var mailboxAddress = new MailboxAddress(addUserReq.RealName, addUserReq.Email);

        var newUser = new User
        {
            OtpEnabled = false,
            PasswordHash = hash,
            Username = addUserReq.Username,
            Roles = "reader",
            Mailboxes = new List<UserMailbox>()
            {
                new UserMailbox
                {
                    DisplayName = addUserReq.RealName,
                    Host = mailboxAddress.Domain,
                    Identity = mailboxAddress.Name,
                }
            },
            Identities = new List<UserIdentity>()
            {
                new UserIdentity
                {
                    DisplayName = addUserReq.RealName,
                    Email = addUserReq.Email,
                }
            }
        };

        data.Users.Add(newUser);
        data.SaveChanges();

        return Results.Ok();
    }
}
