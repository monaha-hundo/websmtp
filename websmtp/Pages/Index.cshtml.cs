using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using SmtpServer.Storage;

namespace websmtp.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, List<string>> Mailboxes { get; set; } = new();
    public int New { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; } = 1;

    [FromQuery]
    public string? Host { get; set; }
    [FromQuery]
    new public string? User { get; set; }

    [FromQuery]
    public int PerPage { get; set; } = 100;

    [FromQuery]
    public int CurrentPage { get; set; } = 1;

    public IndexModel(ILogger<IndexModel> logger,
    IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public IActionResult OnGet(
        [FromQuery] Guid? markAsReadMsgId)
    {

        if (markAsReadMsgId.HasValue)
        {
            _messageStore.MarkAsRead(markAsReadMsgId.Value);
            return new JsonResult(200);
        }

        Mailboxes = _messageStore.Mailboxes();

        Messages = _messageStore.Latest(CurrentPage, PerPage, Host, User);

        New = _messageStore.Count(onlyNew: true, Host, User);
        Total = _messageStore.Count(false);
        Pages = Convert.ToInt32(Math.Ceiling(Decimal.Divide(Total, PerPage)));
        
        if (Pages == 0) Pages = 1;

        return Page();
    }
}
