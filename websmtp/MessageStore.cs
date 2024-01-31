using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using MimeKit;
using Newtonsoft.Json;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using websmtp;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    private static ConcurrentDictionary<Guid, Message> _messagesDict = new ConcurrentDictionary<Guid, Message>();

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

    public List<Message> UnReplied()
    {
        return _messagesDict.Values
                .OrderBy(msg => msg.ReceivedOn)
                .Where(msg => !msg.Replied)
                .ToList();
    }
    public void MarkAsReplied(Message msg, MimeMessage reply)
    {
        _messagesDict[msg.Id].Reply = reply;
        SaveMessage(msg);
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
        var msg = _messagesDict[msgId];
        msg.Read = true;
        SaveMessage(msg);
    }

    public Task LoadMessages()
    {
        _logger.LogInformation("Loading previously received messages.");
        var messageFiles = Directory.GetFiles("messages");
        _logger.LogInformation($"Found {messageFiles.Length} messages in store.");
        var done = 0;
        var sw = new System.Diagnostics.Stopwatch();
        sw.Restart();
        messageFiles
            .AsParallel()
            .ForAll(msgFile =>
            {
                try
                {
                    var json = File.ReadAllText(msgFile);
                    var msg = JsonConvert.DeserializeObject<Message>(json) ?? throw new Exception("Could not parse message.");
                    if (!_messagesDict.TryAdd(msg.Id, msg))
                    {
                        throw new Exception("Could not add loaded message to dictionary.");
                    }
                    done++;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"Could load saved message id #{msgFile}: {ex.Message}");
                    throw;
                }
            });
        sw.Stop();
        _logger.LogInformation($"Loaded {done} off {messageFiles.Length} messages from store in {sw.ElapsedMilliseconds/1000}s.");
        return Task.CompletedTask;
    }

    public Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received message, parsing & saving...");
            var newGuid = Guid.NewGuid();
            var newMessage = new Message(newGuid, buffer);
            _messagesDict.TryAdd(newGuid, newMessage);
            _logger.LogInformation($"Saved message id #{newGuid}.");
            SaveMessage(newMessage);
            return Task.FromResult(SmtpResponse.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not save incoming message: {0}", ex.Message);
            return Task.FromResult(SmtpResponse.TransactionFailed);
            throw;
        }
    }

    private void SaveMessage(Message message)
    {
        var json = JsonConvert.SerializeObject(message);
        var path = Path.Combine("messages", message.Id.ToString("N"));
        File.WriteAllText(path, json);
    }
}