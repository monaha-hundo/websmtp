using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using OtpNet;
using QRCoder;
using System.Security;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp.Endpoints;

public class AccountEndpoints
{
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
        var isAdmin = httpContextAccessor.IsUserAdmin();
        var userId = httpContextAccessor.GetUserId();

        if (!isAdmin && userId != pwdData.UserId)
        {
            throw new SecurityException("Trying to modify a different user from the current user, but not admin.");
        }

        var user = data.Users.Single(u => u.Id == pwdData.UserId);

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

        var isAdmin = httpContextAccessor.IsUserAdmin();
        var userId = httpContextAccessor.GetUserId();

        if (!isAdmin && userId != addMailboxReq.UserId)
        {
            throw new SecurityException("Trying to modify a different user from the current user, but not admin.");
        }

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

        var isAdmin = httpContextAccessor.IsUserAdmin();
        var userId = httpContextAccessor.GetUserId();

        if (!isAdmin && userId != addMailboxReq.UserId)
        {
            throw new SecurityException("Trying to modify a different user from the current user, but not admin.");
        }

        var user = data.Users.Single(u => u.Id == addMailboxReq.UserId);

        var newIdentity = new UserIdentity
        {
            DisplayName = addMailboxReq.DisplayName,
            Email = addMailboxReq.Email
        };

        user.Identities.Add(newIdentity);
        data.SaveChanges();

        return Results.Ok();
    }

    public static IResult RemoveMailbox(
        [FromBody] RemoveUserMailboxRequest removeMailboxReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var isAdmin = httpContextAccessor.IsUserAdmin();
        var userId = httpContextAccessor.GetUserId();

        if (!isAdmin && userId != removeMailboxReq.UserId)
        {
            throw new SecurityException("Trying to modify a different user from the current user, but not admin.");
        }

        var user = data.Users
            .Include(u => u.Mailboxes)
            .Single(u => u.Id == removeMailboxReq.UserId);

        var mbToRemove = user.Mailboxes.Single(m => m.Id == removeMailboxReq.MailboxId);

        user.Mailboxes.Remove(mbToRemove);
        data.SaveChanges();

        return Results.Ok();
    }

    public static IResult RemoveIdentity(
        [FromBody] RemoveUserMailboxRequest removeIDentityReq,
        [FromServices] DataContext data,
        [FromServices] IHttpContextAccessor httpContextAccessor
    )
    {
        var isAdmin = httpContextAccessor.IsUserAdmin();
        var userId = httpContextAccessor.GetUserId();

        if (!isAdmin && userId != removeIDentityReq.UserId)
        {
            throw new SecurityException("Trying to modify a different user from the current user, but not admin.");
        }

        var user = data.Users
            .Include(u => u.Identities)
            .Single(u => u.Id == removeIDentityReq.UserId);

        var idToRemove = user.Identities.Single(m => m.Id == removeIDentityReq.MailboxId);

        user.Identities.Remove(idToRemove);
        data.SaveChanges();

        return Results.Ok();
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
            Mailboxes =
            [
                new UserMailbox
                {
                    DisplayName = addUserReq.RealName,
                    Host = mailboxAddress.Domain,
                    Identity = mailboxAddress.Name,
                }
            ],
            Identities =
            [
                new UserIdentity
                {
                    DisplayName = addUserReq.RealName,
                    Email = addUserReq.Email,
                }
            ]
        };

        data.Users.Add(newUser);
        data.SaveChanges();

        return Results.Ok();
    }
}