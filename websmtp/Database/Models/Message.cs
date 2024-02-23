using System.Buffers;
using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace websmtp.Database.Models;

public interface IMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedOn { get; set; }
    public long Size { get; set; }
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    public string Importance { get; set; }
    public int AttachementsCount { get; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
}

public class Message : IMessage
{
    public Guid Id { get; set; } = Guid.Empty;
    public byte[] Raw { get; set; } = [];
    public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.MinValue;
    public long Size { get; set; }
    [StringLength(1000)] public string Subject { get; set; } = string.Empty;
    [StringLength(1000)] public string From { get; set; } = string.Empty;
    [StringLength(1000)] public string To { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? HtmlContent { get; set; }
    public List<MessageAttachement> Attachements { get; set; } = [];
    public int AttachementsCount { get; set; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    [StringLength(8)] public string Importance { get; set; }

    public Message()
    {

    }

    /// <summary>
    /// Create a new message instance from a byte buffer.
    /// </summary>
    /// <param name="id">An internally generate ID for the message.</param>
    /// <param name="buffer">The raw bytes of the message</param>
    /// <exception cref="Exception"></exception>
    public Message(ReadOnlySequence<byte> buffer)
    {
        Size = buffer.Length;
        Raw = buffer.ToArray<byte>();
        using var memory = new MemoryStream(Raw);
        using var _mimeMessage = MimeMessage.Load(memory) ?? throw new Exception("Could not parse message.");

        ReceivedOn = DateTimeOffset.UtcNow;

        Subject = _mimeMessage.Subject;

        var allFrom = _mimeMessage.From? .Select(f => f.ToString())?.ToList()
            ?? new List<string>(0);

        From = string.Join(',', allFrom);

        var allTo = _mimeMessage.To?.Select(f => f.ToString())?.ToList()
            ?? new List<string>(0);

        To = string.Join(',', allTo);

        var allCc = _mimeMessage.Cc?.Select(f => f.ToString())?.ToList()
            ?? new List<string>(0);

        Cc = string.Join(',', allCc);

        var allBcc = _mimeMessage.Bcc?.Select(f => f.ToString())?.ToList()
            ?? new List<string>(0);

        Bcc = string.Join(',', allBcc);

        Importance = _mimeMessage.Importance switch
        {
            MessageImportance.Low => "Low",
            MessageImportance.Normal => "Normal",
            MessageImportance.High => "High",
            _ => string.Empty,
        };

        var textContent = _mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);
        TextContent = textContent;

        if (_mimeMessage.HtmlBody != null)
        {
            var htmlContent = _mimeMessage.HtmlBody
                ?? throw new Exception("Could not read message HtmlBody");

            var base64HtmlContent = htmlContent != null
                ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent))
                : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Empty));

            HtmlContent = base64HtmlContent;
        }

        if (_mimeMessage.Attachments.Count() > 0)
        {
            Attachements = _mimeMessage.Attachments
                            .Where(a => a.IsAttachment)
                            .Select(a => new MessageAttachement(a))
                            .ToList();

            AttachementsCount = Attachements.Count;
        }
    }

}


public class MessageInfo : IMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedOn { get; set; }
    public long Size { get; set;  }
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    public string Importance { get; set; }
    public int AttachementsCount { get; set; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
    public MessageInfo() { }
    public MessageInfo(Message message)
    {
        Id = message.Id;
        ReceivedOn = message.ReceivedOn;
        Size = message.Size;
        Subject = message.Subject;
        From = message.From;
        To = message.To;
        Cc = message.Cc;
        Bcc = message.Bcc;
        Importance = message.Importance;
        AttachementsCount = message.AttachementsCount;
        Read = message.Read;
        Deleted = message.Deleted;
    }
}

public class MessageAttachement
{
    public Guid Id { get; set; }
    [StringLength(1000)] public string Filename { get; set; } = string.Empty;
    [StringLength(255)] public string MimeType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    [StringLength(1000)] public string ContentId { get; set; } = string.Empty;

    public MessageAttachement(MimeEntity mimeEntity)
    {
        MimeType = mimeEntity.ContentType.MimeType ?? "application/octet-stream";
        Filename = mimeEntity.ContentType.Name
            ?? mimeEntity.ContentDisposition.FileName
            ?? mimeEntity.ContentId;
        ContentId = mimeEntity?.ContentId ?? "";
        using var tempMemory = new MemoryStream();
        var wat = new FormatOptions();
        mimeEntity.WriteTo(wat, tempMemory, true);
        var mimeBytes = tempMemory.ToArray();
        //var mimeBase64 = Convert.ToBase64String(mimeBytes);
        var mimeString = System.Text.Encoding.Default.GetString(mimeBytes);
        Content = mimeString;
    }

    public MessageAttachement()
    {
    }
}