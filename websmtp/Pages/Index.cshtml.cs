using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using websmtp.services;
using websmtp.Services.Models;

namespace websmtp.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public ListResult Listing { get; set; } = null!;

    [FromRoute] public string Mailbox { get; set; } = "inbox";
    [FromQuery] public string Filter { get; set; } = "";

    public IndexModel(ILogger<IndexModel> logger,
    IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public IActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 1000)
    {
        Listing = Mailbox switch
        {
            "inbox" => _messageStore.List(page, perPage, Filter, ListType.Inbox),
            "all" => _messageStore.List(page, perPage, Filter, ListType.All),
            "favorites" => _messageStore.List(page, perPage, Filter, ListType.Favorites),
            "spam" => _messageStore.List(page, perPage, Filter, ListType.Spam),
            "sent" => _messageStore.List(page, perPage, Filter, ListType.Sent),
            "trash" => _messageStore.List(page, perPage, Filter, ListType.Trash),
            _ => throw new Exception("unknown mailbox"),
        };
        return Page();
    }
}
