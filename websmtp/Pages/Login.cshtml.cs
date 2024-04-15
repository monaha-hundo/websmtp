using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;

namespace MyApp.Namespace;

[ValidateAntiForgeryToken]
[AllowAnonymous]
public class LoginModel : PageModel
{

    private readonly IHttpContextAccessor _http;
    private readonly DataContext _data;
    [FromForm] public string Username { get; set; } = string.Empty;
    [FromForm] public string Password { get; set; } = string.Empty;
    public bool Error { get; set; }

    public LoginModel(IHttpContextAccessor http, DataContext data)
    {
        _http = http;
        _data = data;
    }

    public async Task<IActionResult> OnPost()
    {
        var validateParamResult = ValidateParams();
        if (validateParamResult != null)
        {
            return validateParamResult;
        }

        var (pwdValid, user) = CheckPassword();

        if (!pwdValid || user == null)
        {
            Error = true;
            return Page();
        }

        if (user.OtpEnabled)
        {
            var userId = _data.Users
                .Single(u => !u.Deleted && u.Username == Username)
                .Id;
            TempData["userId"] = userId;
            return Redirect("/mfa");
        }

        await _http.SignInAsync(user);
        return Redirect("/inbox");
    }

    private (bool, User?) CheckPassword()
    {
        var passwordHasher = new PasswordHasher();

        var user = _data.Users.SingleOrDefault(u => !u.Deleted && u.Username == Username);

        var isAuth = user != null
            && passwordHasher.VerifyHashedPassword(user.PasswordHash, Password);

        return (isAuth, user);
    }

    private PageResult? ValidateParams()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = true;
            return Page();
        }

        if (_http.HttpContext == null)
        {
            throw new Exception("Not HTTP context available to process login.");
        }

        return null;
    }
}