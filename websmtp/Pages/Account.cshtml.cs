using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.Services.Models;

namespace MyApp.Namespace
{
    [Authorize]
    public class AccountModel : PageModel
    {
        private readonly IReadableMessageStore _messageStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataContext _data;

        public User Profile { get; set; } = null!;

        [FromQuery] public int UserId { get; set; }
        public int CurrentUserId { get; set; }

        public AccountModel(IReadableMessageStore messageStore, IHttpContextAccessor httpContextAccessor, DataContext data)
        {
            _messageStore = messageStore;
            _httpContextAccessor = httpContextAccessor;
            _data = data;
        }

        public void OnGet()
        {
            CurrentUserId = _httpContextAccessor.GetUserId();
            if (UserId == 0)
            {
                UserId = CurrentUserId;
            }
            var isAdmin = User?.IsInRole("admin") ?? false;
            var userIdToFetch = isAdmin
             ? UserId
             : _httpContextAccessor.GetUserId();
            UserId = userIdToFetch;

            Profile = _data.Users.Include(u => u.Identities).Include(u => u.Mailboxes).Single(u => u.Id == userIdToFetch);
        }
    }
}
