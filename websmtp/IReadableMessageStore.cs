using websmtp;
using websmtp.Database.Models;

public interface IReadableMessageStore
{
    public ListResult Latest(int page , int perPage, bool onlyNew, bool showTrash, bool showSpam, string filter);
    public Message Single(Guid msgId, bool includeRaw = false);
    public void MarkAsRead(Guid msgId);
    public void Delete(Guid msgId);
    public void Undelete(Guid msgId);
    public long Count(bool onlyNew);
}
