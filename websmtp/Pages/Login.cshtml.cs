using System.Security.AccessControl;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using websmtp;

namespace MyApp.Namespace
{
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public class LoginModel : PageModel
    {

        public IHttpContextAccessor _http { get; set; }
        public IConfiguration _conf { get; set; }
        [FromForm] public string Username { get; set; } = string.Empty;
        [FromForm] public string Password { get; set; } = string.Empty;
        [FromQuery] public string? ReturnUrl { get; set; }
        public bool Error { get; set; }

        public LoginModel(IHttpContextAccessor http, IConfiguration conf)
        {
            _http = http;
            _conf = conf;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
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

            var passwordHasher = new PasswordHasher();
            var configuredUsername = _conf["Security:Username"] ?? throw new Exception("Please configure the Security:Username settings key.");
            var configuredPasswordHash = _conf["Security:PasswordHash"] ?? throw new Exception("Please configure the Security:PasswordHash settings key.");

            var isAuth = Username == configuredUsername
                && passwordHasher.VerifyHashedPassword(configuredPasswordHash, Password);

            if (isAuth)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username),
                    new Claim(ClaimTypes.Role, "Administrator"),
                };
                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(claimsIdentity);
                await _http.HttpContext.SignInAsync(principal);
                var returnUrl = ReturnUrl ?? "/";
                return Redirect(returnUrl);
            }

            Error = true;
            return Page();
        }
    }
}
