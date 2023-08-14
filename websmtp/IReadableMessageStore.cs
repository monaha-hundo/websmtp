using System.Collections.Concurrent;

public interface IReadableMessageStore
{
    public List<Message> All();
    public List<Message> Latest(bool onlyNew = true, int page = 1, int perPage = 5);
    public List<string> Mailboxes();
    public Message Single(int msgId);
    public void MarkAsRead(int msgId);
    public int Count(bool onlyNew = false);
}
