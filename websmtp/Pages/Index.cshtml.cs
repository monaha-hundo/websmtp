using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using websmtp.Services.Models;

namespace websmtp.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public ListResult Listing { get; set; } = new ListResult();

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
            "all" => _messageStore.Latest(page, perPage, false, false, false, false, false, Filter),
            "favorites" => _messageStore.Latest(page, perPage, false, false, false, true, false, Filter),
            "spam" => _messageStore.Latest(page, perPage, false, false, true, false, false, Filter),
            "sent" => _messageStore.Latest(page, perPage, false, false, false, false, true, Filter),
            "trash" => _messageStore.Latest(page, perPage, false, true, true, false, false, Filter),
            _ => _messageStore.Latest(page, perPage, true, false, false, false, false, Filter),
        };
        return Page();
    }
}
