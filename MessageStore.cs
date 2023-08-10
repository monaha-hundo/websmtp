using System.Buffers;
using System.Collections.Concurrent;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    public static ConcurrentDictionary<Guid, Message> _messages = new();

    private readonly ILogger<MessageStore> _logger;
    public MessageStore(ILogger<MessageStore> logger)
    {
        _logger = logger;
    }

    public ConcurrentDictionary<Guid, Message> All()
    {
        return _messages;
    }
    public Message Single(Guid msgId)
    {
        if (_messages.TryGetValue(msgId, out Message message))
        {
            return message;
        }
        throw new Exception("Message does not exist");
    }

    public async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        var rawMessage = await new StreamReader(stream).ReadToEndAsync()
            ?? throw new Exception("Could not read message.");

        stream.Position = 0;

        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        var textContent = mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);

        var attachments = mimeMessage.Attachments
            .Where(a=>a.IsAttachment)
            .Select(a => new MessageAttachement(a))
            .ToList();

        var messageToStore = new Message
        {
            ReceivedOn = mimeMessage.Date,
            Subject = mimeMessage.Subject,
            From = string.Join(',', mimeMessage.From.Select(f => f.ToString())),
            To = string.Join(',', mimeMessage.To.Select(f => f.ToString())),
            TextContent = textContent,
            Attachements = attachments,
            Raw = rawMessage
        };

        _messages.TryAdd(messageToStore.Id, messageToStore);

        // foreach (var address in message.To)
        // {
        //     var addr = address.ToString();
        //     var existingMailbox = _messages.ContainsKey(addr);
        //     if (existingMailbox)
        //     {
        //         _messages[addr].Add(message);
        //     }
        //     else
        //     {
        //         var newMailbox = new List<MimeKit.MimeMessage>() { message };
        //         var added = _messages.TryAdd(addr, newMailbox);
        //         if (added)
        //         {
        //             _logger.LogInformation($"Message added to mailbox: '{addr}'.");
        //         }
        //         else
        //         {
        //             _logger.LogCritical($"Message could not be added to a mailbox: '{addr}'.");
        //         }
        //     }
        // }

        return SmtpResponse.Ok;
    }
}