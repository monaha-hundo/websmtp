using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.Services.Models;

namespace MyApp.Namespace;


public class ListUserResult
{
    public List<User> Users { get; set; } = null!;
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Total { get; set; }
}

[Authorize(Roles = "admin")]
public class UsersModel : PageModel
{
    private readonly IReadableMessageStore _messageStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DataContext _data;

    public ListUserResult Users { get; set; } = null!;

    [FromQuery] public int? CurrentPage { get; set; }
    [FromQuery] public int? PerPage { get; set; }
    [FromQuery] public string Filter { get; set; } = string.Empty;
    public ListResult Listing { get; set; } = new ListResult();

    public UsersModel(IHttpContextAccessor httpContextAccessor,
        DataContext data,
        IReadableMessageStore messageStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _data = data;
        _messageStore = messageStore;
    }

    public void OnGet()
    {
        CurrentPage ??= 1;
        PerPage ??= 25;
        var userId = _httpContextAccessor.GetUserId();
        Listing = _messageStore.Latest(1, 1, true, false, false, false, false, string.Empty);
        
        var userCount = string.IsNullOrWhiteSpace(Filter)
            ? _data.Users.Count()
            : _data.Users.Count(u => u.Username.Contains(Filter));

        var users = string.IsNullOrWhiteSpace(Filter)
            ? _data.Users.OrderByDescending(u => u.Id).Skip((CurrentPage.Value - 1) * PerPage.Value).Take(PerPage.Value).ToList()
            : _data.Users.OrderByDescending(u => u.Id).Where(u => u.Username.Contains(Filter)).Skip((CurrentPage.Value - 1) * PerPage.Value).Take(PerPage.Value).ToList();

        Users = new ListUserResult
        {
            Page = CurrentPage.Value,
            PerPage = PerPage.Value,
            Total = userCount,
            Users = users
        };
    }
}