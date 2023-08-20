using System.Buffers;
using System.Collections.Concurrent;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using websmtp;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    private static ConcurrentDictionary<Guid, Message> _messagesDict = new ConcurrentDictionary<Guid, Message>();

    //public static List<Message> _messages => _messagesDict.Values.ToList();

    private readonly ILogger<MessageStore> _logger;
    public MessageStore(ILogger<MessageStore> logger)
    {
        _logger = logger;
    }

    public List<Message> All()
    {
        return _messagesDict.Values.ToList();
    }

    public List<Message> Latest(
        bool onlyNew = true,
        int page = 1,
        int perPage = 5,
        string? filterByHost = null,
        string? filterByUser = null)
    {

        if (onlyNew)
        {
            return _messagesDict.Values
                .OrderByDescending(msg => msg.ReceivedOn)
                .Where(msg => msg.Read == !onlyNew)
                .Where(msg => string.IsNullOrWhiteSpace(filterByHost)
                    ? true : msg.To.Contains(filterByHost))
                .Where(msg => string.IsNullOrWhiteSpace(filterByUser)
                    ? true : msg.To.Contains(filterByUser + '@'))
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();
        }

        return _messagesDict.Values
            .OrderByDescending(msg => msg.ReceivedOn)
                .Where(msg => string.IsNullOrWhiteSpace(filterByHost)
                    ? true : msg.To.Contains(filterByHost))
                .Where(msg => string.IsNullOrWhiteSpace(filterByUser)
                    ? true : msg.To.Contains(filterByUser + '@'))
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToList();
    }

    public int Count(bool onlyNew = false)
    {
        if (onlyNew)
        {
            return _messagesDict.Values.Count(msg => !msg.Read);
        }
        else
        {
            return _messagesDict.Count;
        }
    }

    public Dictionary<string, List<string>> Mailboxes()
    {
        var mailboxes = _messagesDict.Values
            .Select(msg => new { Host = msg.To.Split('@')[1], User = msg.To.Split('@')[0] })
            .GroupBy(msg => msg.Host)
            .ToDictionary(
                hostGrp => hostGrp.First().Host,
                hostGrp => hostGrp.Select(hg => hg.User)
                    .GroupBy(u => u)
                    .Select(grp => grp.First())
                    .ToList())
            ?? throw new Exception("Could not generate mailboxes.");

        return mailboxes;
    }

    public Message Single(Guid msgId)
    {
        return _messagesDict[msgId];
    }

    public void MarkAsRead(Guid msgId)
    {
        _messagesDict[msgId].Read = true;
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

        var msgBytes = stream.ToArray();

        var newGuid = Guid.NewGuid();

        var newMessage = new Message(newGuid, msgBytes);
        _messagesDict.TryAdd(newGuid, newMessage);
        // var maxRetry = 3;
        //var retries = 0;
        // while (!_messagesDict.TryAdd(newGuid, newMessage) && retries <= maxRetry)
        // {
        //     retries++`
        //     await Task.Delay(100);
        // }

        return SmtpResponse.Ok;
    }

}