using System.Security.Claims;

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
}
