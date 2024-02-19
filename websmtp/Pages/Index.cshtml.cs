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

    [FromQuery]
    public bool OnlyNew { get; set; } = false;

    public IndexModel(ILogger<IndexModel> logger,
    IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public IActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 25,
        [FromQuery] string filter = "")
    {
        Listing = _messageStore.Latest(page, perPage, OnlyNew, filter);
        return Page();
    }
}
