using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.services;

namespace MyApp.Namespace;


public class ListUserResult
{
    public List<User> Users { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Total { get; set; }
}

[Authorize(Roles = "admin")]
public class AdminModel : PageModel
{
    private readonly IReadableMessageStore _messageStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DataContext _data;

    public ListUserResult Users { get; set; }

    [BindProperty] public int? Page { get; set; }
    [BindProperty] public int? PerPage { get; set; }
    [BindProperty] public string Filter { get; set; }
    public ListResult Listing { get; set; } = new ListResult();

    public AdminModel(IHttpContextAccessor httpContextAccessor,
        DataContext data,
        IReadableMessageStore messageStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _data = data;
        _messageStore = messageStore;
    }

    public void OnGet()
    {
        Page = Page.HasValue ? Page.Value : 1;
        PerPage = PerPage.HasValue ? PerPage.Value : 25;
        var userId = _httpContextAccessor.GetUserId();
        Listing = _messageStore.Latest(1, 1, true, false, false, false, false, string.Empty);
        var userCount = _data.Users.Count();
        var users = string.IsNullOrWhiteSpace(Filter)
            ? _data.Users.Include(u => u.Mailboxes).Include(u => u.Identities).Skip((Page.Value - 1) * PerPage.Value).Take(PerPage.Value).ToList()
            : _data.Users.Include(u => u.Mailboxes).Include(u => u.Identities).Where(u => u.Username.Contains(Filter)).Skip((Page.Value - 1) * PerPage.Value).Take(PerPage.Value).ToList();

        Users = new ListUserResult
        {
            Page = Page.Value,
            PerPage = PerPage.Value,
            Total = userCount,
            Users = users
        };
    }
}