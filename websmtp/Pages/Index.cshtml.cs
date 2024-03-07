using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public IActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 1000)
    {
        switch (Mailbox)
        {
            default:
            case "inbox":
                Listing = _messageStore.Latest(page, perPage, true, false, false, Filter);
                break;

            case "all":
                Listing = _messageStore.Latest(page, perPage, false, false, false, Filter);
                break;

            case "spam":
                Listing = _messageStore.Latest(page, perPage, false, false, true, Filter);
                break;

            case "trash":
                Listing = _messageStore.Latest(page, perPage, false, true, true, Filter);
                break;
        }
        return Page();
    }
}
