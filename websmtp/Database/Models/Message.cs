using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MimeKit;
using websmtp.Services.Models;

namespace websmtp.Database.Models;

public class Message : IMessage
{
    public Guid Id { get; set; } = Guid.Empty;
    public int UserId { get; set; }
    [ForeignKey("UserId")]public User User { get; set; } = null!;
    public bool Sent { get; set; }
    public Guid RawMessageId { get; set; } = Guid.Empty;
    [ForeignKey("RawMessageId")] public RawMessage RawMessage { get; set; } = null!; // A message will always have an associated raw message.
    public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.MinValue;
    [StringLength(1000)] public string Subject { get; set; } = string.Empty;
    [StringLength(1000)] public string From { get; set; } = string.Empty;
    [StringLength(1000)] public string To { get; set; } = string.Empty;
    [StringLength(1000)] public string Cc { get; set; } = string.Empty;
    [StringLength(1000)] public string Bcc { get; set; } = string.Empty;
    [StringLength(8)] public string Importance { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? HtmlContent { get; set; }
    public List<MessageAttachement> Attachements { get; set; } = [];
    public int AttachementsCount { get; set; }
    public bool Stared { get; set; }
    public bool Read { get; set; }
    public bool Deleted { get; set; }
    public bool IsSpam { get; set; }
    public string? Headers { get; set; }

    public Message()
    {

    }

    public Message(MimeMessage _mimeMessage)
    {
        ReceivedOn = DateTimeOffset.UtcNow;

        Subject = _mimeMessage.Subject;

        var allFrom = _mimeMessage.From?.Select(f => f.ToString())?.ToList() ?? [];

        From = string.Join(',', allFrom);

        var allTo = _mimeMessage.To?.Select(f => f.ToString())?.ToList() ?? [];

        To = string.Join(',', allTo);

        var allCc = _mimeMessage.Cc?.Select(f => f.ToString())?.ToList() ?? [];

        Cc = string.Join(',', allCc);

        var allBcc = _mimeMessage.Bcc?.Select(f => f.ToString())?.ToList() ?? [];

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

            var bodyParts = _mimeMessage.BodyParts
                .Where(a => !string.IsNullOrEmpty(a.ContentId))
                .Select(a => new MessageAttachement(a))
                .ToList();

            var realAttachments = _mimeMessage.Attachments
                .Where(a => a.IsAttachment)
                .Select(a => new MessageAttachement(a))
                .ToList();

            var attachments = bodyParts.Concat(realAttachments).ToList();

            foreach (var attachment in attachments.Where(a => !string.IsNullOrWhiteSpace(a.ContentId)))
            {
                var indexOfCid = htmlContent.IndexOf(attachment.ContentId);
                var foundCid = indexOfCid > -1;
                if (foundCid)
                {
                    htmlContent = htmlContent.Replace(
                        "cid:" + attachment.ContentId,
                        string.Format("data:{0};base64,{1}", attachment.MimeType, attachment.Content));
                }
            }

            var base64HtmlContent = htmlContent != null
                ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent))
                : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Empty));

            HtmlContent = base64HtmlContent;
        }

        if (_mimeMessage.Attachments.Any())
        {
            Attachements = _mimeMessage.Attachments
                            .Where(a => a.IsAttachment)
                            .Select(a => new MessageAttachement(a))
                            .ToList();

            AttachementsCount = Attachements.Count;
        }
    }

}