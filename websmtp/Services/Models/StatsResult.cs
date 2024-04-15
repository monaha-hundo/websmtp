namespace websmtp.Services.Models;

public class StatsResult
{
    public int Inbox { get; set; }
    public int All { get; set; }
    public int Favs { get; set; }
    public int Spam { get; set; }
    public int Trash { get; set; }
    public bool AllHasNew { get; set; }
    public bool SpamHasNew { get; set; }
    public bool TrashHasNew { get; set; }
}