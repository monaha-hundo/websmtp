using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OtpNet;
using System.Security.Claims;
using websmtp;
using websmtp.Database;

namespace MyApp.Namespace;

[ValidateAntiForgeryToken]
[AllowAnonymous]
public class LoginModel : PageModel
{

    public IHttpContextAccessor _http { get; set; }
    public IConfiguration _conf { get; set; }
    private readonly DataContext _data;
    [FromForm] public string Username { get; set; } = string.Empty;
    [FromForm] public string Password { get; set; } = string.Empty;
    [FromForm] public string OTP { get; set; } = string.Empty;
    [FromQuery] public string? ReturnUrl { get; set; }
    public bool Error { get; set; }
    public bool MfaEnabled => _conf.GetValue<bool>("Security:MfaEnabled");

    public LoginModel(IHttpContextAccessor http, IConfiguration conf, DataContext data)
    {
        _http = http;
        _conf = conf;
        _data = data;
    }

    public async Task<IActionResult> OnPost()
    {
        var validateParamResult = ValidateParams();
        if (validateParamResult != null)
        {
            return validateParamResult;
        }

        var isAuth = CheckPassword() && CheckMfa();

        if (isAuth)
        {
            await SignIn();
            //var returnUrl = ReturnUrl ?? "/";
            return Redirect("/inbox");
        }

        Error = true;
        return Page();
    }

    private async Task SignIn()
    {
        var user = _data.Users.Single(u => u.Username == Username);

        if (_http == null || _http.HttpContext == null || user == null)
        {
            throw new Exception("Cannot sign in, http context is not found or user was null.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var roles = user.Roles
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        roles.ForEach(r => claims.Add(new Claim(ClaimTypes.Role, r)));

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(claimsIdentity);

        await _http.HttpContext.SignInAsync(principal);
    }

    private bool CheckMfa()
    {
        var user = _data.Users.Single(u => !u.Deleted && u.Username == Username);

        if (user.OtpEnabled)
        {
            var secretBytes = Base32Encoding.ToBytes(user.OtpSecret);
            var totp = new Totp(secretBytes);
            var result = totp.VerifyTotp(OTP, out var timeSteps);
            return result;
        }

        return true;
    }

    private bool CheckPassword()
    {
        var passwordHasher = new PasswordHasher();
        var configuredUsername = _conf["Security:Username"] ?? throw new Exception("Please configure the Security:Username settings key.");
        var configuredPasswordHash = _conf["Security:PasswordHash"] ?? throw new Exception("Please configure the Security:PasswordHash settings key.");

        var user = _data.Users.SingleOrDefault(u => !u.Deleted && u.Username == Username);

        var isAuth = user != null
            && passwordHasher.VerifyHashedPassword(user.PasswordHash, Password);

        return isAuth;
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