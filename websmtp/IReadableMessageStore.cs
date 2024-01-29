using System.Collections.Concurrent;
using websmtp;

public interface IReadableMessageStore
{
    public List<Message> All();
    public List<Message> Latest(bool onlyNew = true, int page = 1, int perPage = 5, string? filterByHost = null, string? filterByUser = null);
    public Dictionary<string,List<string>> Mailboxes();
    public Message Single(Guid msgId);
    public void MarkAsRead(Guid msgId);
    public int Count(bool onlyNew = false);

    public Task LoadMessages();
}
