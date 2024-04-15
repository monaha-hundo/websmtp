namespace websmtp.Services.Models;

public class ListResult
{
    public int Count { get; set; }
    public List<MessageInfo> Messages { get; set; } = [];

    public ListResult(List<MessageInfo> messages)
    {
        Count = messages.Count;
        Messages = messages;
    }
}