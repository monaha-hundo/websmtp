using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;

namespace MyApp.Namespace
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly IReadableMessageStore _messageStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataContext _data;

        public User User { get; set; }

        private int GetUserGuid()
        {
            try
            {
                var rawId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
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
        public SettingsModel(IReadableMessageStore messageStore, IHttpContextAccessor httpContextAccessor, DataContext data)
        {
            _messageStore = messageStore;
            _httpContextAccessor = httpContextAccessor;
            _data = data;
        }

        public ListResult Listing { get; set; } = new ListResult();
        public void OnGet()
        {
            var userId = GetUserGuid();
            User = _data.Users.Include(u=>u.Mailboxes).Single(u => u.Id == userId);
            Listing = _messageStore.Latest(1, 1, true, false, false, false, false, string.Empty);
        }
    }
}
