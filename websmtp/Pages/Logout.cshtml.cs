using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private readonly IHttpContextAccessor _http;

        public LogoutModel(IHttpContextAccessor http)
        {
            _http = http;
        }

        public async Task<IActionResult> OnGet()
        {
            if(!User?.Identity?.IsAuthenticated ?? false)
            {
                throw new Exception("Trying to log out, but request has no user authentication data.");
            }

            if (_http.HttpContext == null)
            {
                throw new Exception("No HTTP context available to process login.");
            }

            await _http.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/");
        }
    }
}