using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using websmtp.Database.Models;

namespace websmtp.Pages;

[Authorize]
public class MessageViewModel : PageModel
{
    [FromQuery] public Guid MsgId { get; set; } = Guid.Empty;
    [FromQuery] public bool ShowRaw { get; set; } = false;
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public Message Message { get; set; }

    public MessageViewModel(
        ILogger<IndexModel> logger,
        IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
        Message = new Message();
    }

    public void OnGet()
    {
        Message = _messageStore.Single(MsgId, ShowRaw);
    }
}
