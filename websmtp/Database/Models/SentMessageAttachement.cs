using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace websmtp.Database.Models;

public class SentMessageAttachement
{
    public Guid Id { get; set; }
    [StringLength(1000)] public string Filename { get; set; } = string.Empty;
    [StringLength(255)] public string MimeType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    [StringLength(1000)] public string ContentId { get; set; } = string.Empty;

    public SentMessageAttachement(MimeEntity mimeEntity)
    {
        ArgumentNullException.ThrowIfNull(mimeEntity);
        using var tempMemory = new MemoryStream();
        var wat = new FormatOptions();
        mimeEntity.WriteTo(wat, tempMemory, true);
        MimeType = mimeEntity.ContentType.MimeType ?? "application/octet-stream";
        Filename = mimeEntity.ContentType.Name
            ?? mimeEntity.ContentDisposition.FileName
            ?? mimeEntity.ContentId;
        ContentId = mimeEntity?.ContentId ?? "";
        var mimeBytes = tempMemory.ToArray();
        var mimeString = System.Text.Encoding.Default.GetString(mimeBytes);
        Content = mimeString;
    }

    public SentMessageAttachement()
    {
    }
}