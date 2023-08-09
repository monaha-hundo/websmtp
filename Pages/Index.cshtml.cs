﻿using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using SmtpServer.Storage;

namespace websmtp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadableMessageStore _messageStore;

    public ConcurrentDictionary<Guid, Message> Messages { get; set; }

    public IndexModel(ILogger<IndexModel> logger,
    IReadableMessageStore messageStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
    }

    public void OnGet()
    {
        Messages = _messageStore.All();
    }
}
