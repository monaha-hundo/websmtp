using websmtp.Database.Models;

namespace websmtp;

public class ListResult
{
    public int Count { get; set; }
    public int New { get; set; }
    public int Total { get; set; }
    public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>(0);
}