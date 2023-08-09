using System.Collections.Concurrent;

public interface IReadableMessageStore
{
    public ConcurrentDictionary<Guid, Message> All();
    public Message Single(Guid msgId);
}
