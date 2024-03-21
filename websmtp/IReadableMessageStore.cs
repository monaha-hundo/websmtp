using websmtp.Database.Models;
using websmtp.services;

public interface IReadableMessageStore
{
    public ListResult Latest(int page, int perPage, bool onlyNew, bool showTrash, bool showSpam, bool onlyFavs, bool showSent, string filter);
    public Message Single(Guid msgId, bool includeRaw = false);
    public void MarkAsRead(List<Guid> messagesIds);
    public void MarkAsUnread(List<Guid> messagesIds);
    public void Delete(List<Guid> messagesIds);
    public void Undelete(List<Guid> messagesIds);
    public void Star(List<Guid> messagesIds);
    public void Unstar(List<Guid> messagesIds);
    public void Spam(List<Guid> messagesIds);
    public void NotSpam(List<Guid> messagesIds);
    public long Count(bool onlyNew);
}
