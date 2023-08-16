using System.Buffers;
using System.Collections.Concurrent;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    public static List<Message> _messages = new List<Message>(1000);

    private readonly ILogger<MessageStore> _logger;
    public MessageStore(ILogger<MessageStore> logger)
    {
        _logger = logger;
    }

    public List<Message> All()
    {
        return _messages;
    }

    public List<Message> Latest(
        bool onlyNew = true,
        int page = 1,
        int perPage = 5)
    {
        if (onlyNew)
        {
            return _messages
                .OrderByDescending(msg => msg.ReceivedOn)
                .Where(msg => msg.Read == !onlyNew)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();
        }

        return _messages
            .OrderByDescending(msg => msg.ReceivedOn)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToList();
    }

    public int Count(bool onlyNew = false)
    {
        if (onlyNew)
        {
            return _messages.Count(msg => !msg.Read);
        }
        else
        {
            return _messages.Count;
        }
    }

    public List<string> Mailboxes()
    {
        lock (_messages)
        {
            return _messages
                .Select(msg => msg.To)
                .GroupBy(from => from)
                .Select(fromGrp => fromGrp.First())
                .ToList();
        }
    }

    public Message Single(int msgId)
    {
        if (_messages[msgId] != null)
        {
            return _messages[msgId];
        }
        throw new Exception("Message does not exist");
    }

    public async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        using var streamReader = new StreamReader(stream);
        var rawMessage = await streamReader.ReadToEndAsync()
            ?? throw new Exception("Could not read message.");

        stream.Position = 0;

        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        var textContent = mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);
        var htmlContent = mimeMessage.HtmlBody;

        htmlContent = htmlContent?.Replace("</body>", $@"
                         <script>
                            setInterval(()=>window.parent.postMessage({{ ""type"": ""frame-resized"", ""value"": document.documentElement.clientHeight  }}, '*'), 100);
                         </script>
                         </body>
                    ");

        var bodyParts = mimeMessage.BodyParts
            .Where(a => !string.IsNullOrEmpty(a.ContentId))
            .Select(a => new MessageAttachement(a))
            .ToList();

        var realAttachments = mimeMessage.Attachments
            //.Where(a => a.IsAttachment)
            .Select(a => new MessageAttachement(a))
            .ToList();

        var attachments = bodyParts.Concat(realAttachments).ToList();

        foreach (var attachment in attachments.Where(a => !string.IsNullOrWhiteSpace(a.ContentId)))
        {
            var indexOfCid = htmlContent.IndexOf(attachment.ContentId);
            var foundCid = indexOfCid > -1;
            if (foundCid)
            {
                htmlContent = htmlContent.Replace(
                    "cid:" + attachment.ContentId,
                    string.Format("data:{0};base64,{1}", attachment.MimeType, attachment.Content));
            }
        }

        var base64HtmlContent = htmlContent != null ?
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent))
            : null;
            
        var messageToStore = new Message
        {
            ReceivedOn = mimeMessage.Date,
            Subject = mimeMessage.Subject,
            From = string.Join(',', mimeMessage.From.Select(f => f.ToString())),
            To = string.Join(',', mimeMessage.To.Select(f => f.ToString())),
            TextContent = textContent,
            HtmlContent = base64HtmlContent,
            Attachements = attachments,
            Raw = rawMessage
        };

        lock (_messages)
        {
            _messages.Add(messageToStore);
        }

        return SmtpResponse.Ok;
    }

    public void MarkAsRead(int msgId)
    {
        _messages[msgId].Read = true;
    }
}