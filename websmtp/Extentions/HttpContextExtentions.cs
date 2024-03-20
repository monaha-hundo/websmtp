using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using websmtp.Database.Models;

namespace websmtp;

public static class HttpContextExtentions
{
    public static int GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            var rawId = httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new Exception("Could not fund user id claim");
            if (int.TryParse(rawId, out var userId))
            {
                return userId;
            }
            throw new Exception("Could not cast user id claim to int.");
        }
        catch (System.Exception ex)
        {
            throw new Exception("Could not get user guid: ", ex);
        }
    }

    public static async Task SignInAsync(this IHttpContextAccessor httpContextAccessor, User user)
    {
        if (httpContextAccessor == null || httpContextAccessor.HttpContext == null || user == null)
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

        await httpContextAccessor.HttpContext.SignInAsync(principal);
    }
}
