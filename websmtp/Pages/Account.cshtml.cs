using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.services;

namespace MyApp.Namespace
{
    [Authorize]
    public class AccountModel : PageModel
    {
        private readonly IReadableMessageStore _messageStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataContext _data;

        public ListResult Listing { get; set; } = new ListResult();

        public User Profile { get; set; } = null!;

        public AccountModel(IReadableMessageStore messageStore, IHttpContextAccessor httpContextAccessor, DataContext data)
        {
            _messageStore = messageStore;
            _httpContextAccessor = httpContextAccessor;
            _data = data;
        }

        public void OnGet()
        {
            var userId = _httpContextAccessor.GetUserId();
            Profile = _data.Users.Include(u => u.Identities).Include(u => u.Mailboxes).Single(u => u.Id == userId);
            Listing = _messageStore.Latest(1, 1, true, false, false, false, false, string.Empty);
        }
    }
}
