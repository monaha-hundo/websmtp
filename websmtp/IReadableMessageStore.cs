using websmtp;
using websmtp.Database.Models;

public interface IReadableMessageStore
{
    public ListResult Latest(int page , int perPage, bool onlyNew, string filter);
    public Message Single(Guid msgId);
    public void MarkAsRead(Guid msgId);
    public long Count(bool onlyNew);
}
