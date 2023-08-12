using System.Collections.Concurrent;

public interface IReadableMessageStore
{
    public ConcurrentDictionary<Guid, Message> All();
    public Dictionary<Guid, Message> Latest(bool onlyNew = true, int page = 1, int perPage = 5);
    public List<string> Mailboxes();
    public Message Single(Guid msgId);
    public void MarkAsRead(Guid msgId);
    public int Count(bool onlyNew = false);
}
