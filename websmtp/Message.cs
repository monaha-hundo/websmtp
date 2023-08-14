using MimeKit;

public class Message
{
    public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.Now;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string Raw { get; set; } = string.Empty;
    public List<MessageAttachement> Attachements { get; set; } = new();
    public int Size => System.Text.Encoding.Default.GetByteCount(Raw);
    public bool Read { get; set; }
}

public class MessageAttachement
{
    public string Filename { get; set; }
    public string MimeType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentId { get; set; }

    public MessageAttachement(MimeKit.MimeEntity mimeEntity)
    {
        MimeType = mimeEntity?.ContentType?.MimeType ?? "application/octet-stream";
        Filename = mimeEntity?.ContentDisposition.FileName
            ?? mimeEntity.ContentId;
        ContentId = mimeEntity.ContentId;
        using (var tempMemory = new MemoryStream())
        {
            var wat = new FormatOptions();
            mimeEntity.WriteTo(wat, tempMemory, true);
            var mimeBytes = tempMemory.ToArray();
            //var mimeBase64 = Convert.ToBase64String(mimeBytes);
            var phase2Bytes = System.Text.Encoding.Default.GetString(mimeBytes);
            Content = phase2Bytes;
        }
    }
}
