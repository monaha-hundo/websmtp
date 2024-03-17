namespace websmtp;

public interface IMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedOn { get; set; }
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    public string Importance { get; set; }
    public int AttachementsCount { get; }
    public bool Stared { get; set; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
    public bool DkimFailed { get; set; }
    public SpfVerifyResult SpfStatus { get; set; }
    public bool DmarcFailed { get; set; }
    public bool Spam => DkimFailed || (SpfStatus != SpfVerifyResult.Pass) || DmarcFailed;
}
