using websmtp.Database.Models;
using websmtp.services;
using websmtp.Services.Models;

public interface IReadableMessageStore
{
    public StatsResult Stats();
    public ListResult List(int page, int perPage, string filter, ListType listType);
    public Message Single(Guid msgId, bool includeRaw = false);
    public void MarkAsRead(List<Guid> messagesIds);
    public void MarkAsUnread(List<Guid> messagesIds);
    public void Delete(List<Guid> messagesIds);
    public void Undelete(List<Guid> messagesIds);
    public void Star(List<Guid> messagesIds);
    public void Unstar(List<Guid> messagesIds);
    public void Spam(List<Guid> messagesIds);
    public void NotSpam(List<Guid> messagesIds);
    public Task TrainSpam(List<Guid> msgIds, bool isSpam);
    public long Count(bool onlyNew);
}
