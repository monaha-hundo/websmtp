using System.Security.AccessControl;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {

        public IHttpContextAccessor _http { get; set; }
        [FromForm] public string Username { get; set; } = string.Empty;
        [FromForm] public string Password { get; set; } = string.Empty;
        [FromQuery] public string? ReturnUrl { get; set; }
        public bool Error { get; set; }

        public LoginModel(IHttpContextAccessor http)
        {
            _http = http;
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
            if (Username == Password)
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
