using websmtp.Database.Models;

namespace websmtp.Services.Models;

public class MessageInfo : IMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedOn { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
    public int AttachementsCount { get; set; }
    public bool Stared { get; set; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
    public bool IsSpam { get; set; }
    public string? Headers { get; set; }
    public MessageInfo() { }
    public MessageInfo(Message message)
    {
        Id = message.Id;
        AttachementsCount = message.AttachementsCount;
        From = message.From;
        Stared = message.Stared;
        Read = message.Read;
        Deleted = message.Deleted;
        IsSpam = message.IsSpam;
        ReceivedOn = message.ReceivedOn;
        Subject = message.Subject;
        To = message.To;
        Cc = message.Cc;
        Bcc = message.Bcc;
        Importance = message.Importance;
    }
}
