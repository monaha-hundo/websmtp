using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using SmtpServer.Storage;

namespace websmtp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public Dictionary<Guid, Message> Messages { get; set; }
    public List<string> Mailboxes { get; set; }
    public int New { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; } = 1;

    [FromQuery]
    public bool OnlyNew { get; set; } = true;

    [FromQuery]
    public int PerPage { get; set; } = 5;

    [FromQuery]
    public int Page { get; set; }

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
        Messages = _messageStore.Latest(OnlyNew, Page, PerPage);
        New = _messageStore.Count(onlyNew: true);
        Total = _messageStore.Count(onlyNew: false);
        if (OnlyNew == true)
        {
            Pages = Convert.ToInt32(Math.Ceiling(Decimal.Divide(New, PerPage)));
        }
        else
        {
            Pages = Convert.ToInt32(Math.Ceiling(Decimal.Divide(Total, PerPage)));
        }
        if (Pages == 0) Pages = 1;

        return Page();
    }
}
