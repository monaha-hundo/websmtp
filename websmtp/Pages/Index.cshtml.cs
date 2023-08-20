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

    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, List<string>> Mailboxes { get; set; } = new();
    public int New { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; } = 1;

    [FromQuery]
    public string? Host { get; set; }
    [FromQuery]
    public string? User { get; set; }

    [FromQuery]
    public bool OnlyNew { get; set; } = true;

    [FromQuery]
    public int PerPage { get; set; } = 5;

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

        Messages = _messageStore.Latest(OnlyNew, CurrentPage, PerPage, Host, User);

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
