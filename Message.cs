public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.Now;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string Raw { get; set; } = string.Empty;
    public List<MessageAttachement> Attachements { get; set; } = new();
    public int Size => System.Text.Encoding.Default.GetByteCount(Raw);
    public bool Read { get; set; }
}

public class MessageAttachement
{
    public string Filename { get; set; }
    public byte[] Content { get; set; } = new byte[0];

    public MessageAttachement() { }
    public MessageAttachement(MimeKit.MimeEntity mimeEntity)
    {
        Filename = mimeEntity.ContentDisposition.FileName;
        using var tempMemory = new MemoryStream();
        mimeEntity.WriteTo(tempMemory);
        Content = tempMemory.ToArray();
    }
}
