using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using SmtpServer.Storage;

namespace websmtp.Pages;

public class MessageViewModel : PageModel
{
    [FromQuery]
    public int MsgId { get; set; }
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public Message Message { get; set; }

    public MessageViewModel(ILogger<IndexModel> logger,
    IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public void OnGet()
    {
        Message = _messageStore.Single(MsgId);
    }
}
