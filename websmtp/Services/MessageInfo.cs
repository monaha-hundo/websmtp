using websmtp.Database.Models;

namespace websmtp.services;

public class MessageInfo : IMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedOn { get; set; }
    //public long Size { get; set;  }
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
        ReceivedOn = message.ReceivedOn;
        Subject = message.Subject;
        From = message.From;
        To = message.To;
        Cc = message.Cc;
        Bcc = message.Bcc;
        Importance = message.Importance;
        AttachementsCount = message.AttachementsCount;
        Read = message.Read;
        Deleted = message.Deleted;
        Stared = message.Stared;
        IsSpam = message.IsSpam;
        Headers = message.Headers;
    }
}
