using websmtp.Database.Models;

namespace websmtp;

public class ListResult
{
    public int Count { get; set; }
    public int New { get; set; }
    public int Spam { get; set; }
    public int Deleted { get; set; }
    public int Total { get; set; }
    public bool AllHasNew { get; set; }
    public bool SpamHasNew { get; set; }
    public bool TrashHasNew { get; set; }
    public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>(0);
}