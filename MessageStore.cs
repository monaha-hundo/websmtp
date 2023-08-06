using System.Buffers;
using System.Collections.Concurrent;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

public interface IReadableStore
{
    public ConcurrentDictionary<string, List<MimeKit.MimeMessage>> All();
}

public class MessageStore : IMessageStore, IReadableStore
{
    public static ConcurrentDictionary<string, List<MimeKit.MimeMessage>> _messages = new();

    private readonly ILogger<MessageStore> _logger;
    public MessageStore(ILogger<MessageStore> logger)
    {
        _logger = logger;
    }

    public ConcurrentDictionary<string, List<MimeKit.MimeMessage>> All()
    {
        return _messages;
    }

    public async Task<SmtpResponse> SaveAsync(
        SmtpServer.ISessionContext context,
        SmtpServer.IMessageTransaction transaction,
        System.Buffers.ReadOnlySequence<byte> buffer,
        System.Threading.CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        //_logger.LogInformation(await new StreamReader(stream).ReadToEndAsync());

        //stream.Position = 0;

        var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);

        //_logger.LogInformation(message.GetTextBody(MimeKit.Text.TextFormat.Text));

        foreach (var address in message.To)
        {
            var addr = address.ToString();
            var existingMailbox = _messages.ContainsKey(addr);
            if (existingMailbox)
            {
                _messages[addr].Add(message);
            }
            else
            {
                var newMailbox = new List<MimeKit.MimeMessage>() { message };
                var added = _messages.TryAdd(addr, newMailbox);
                if (added)
                {
                    _logger.LogInformation($"Message added to mailbox: '{addr}'.");
                }
                else
                {
                    _logger.LogCritical($"Message could not be added to a mailbox: '{addr}'.");
                }
            }
        }

        return SmtpResponse.Ok;
    }
}