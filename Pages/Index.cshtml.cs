using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using SmtpServer.Storage;

namespace websmtp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableStore _messageStore;

    public ConcurrentDictionary<string, List<MimeKit.MimeMessage>> Mailboxes { get; set; }

    public IndexModel(ILogger<IndexModel> logger,
    IReadableStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public void OnGet()
    {
        Mailboxes = _messageStore.All();
    }
}
