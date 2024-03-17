using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MimeKit;

namespace websmtp.Database.Models;

public class SentMessage : IMessage
{
    public Guid Id { get; set; } = Guid.Empty;

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
    public bool DkimFailed { get; set; }
    public SpfVerifyResult SpfStatus { get; set; }
    public bool DmarcFailed { get; set; }

    public SentMessage()
    {

    }

    public SentMessage(MimeMessage mimeMessage)
    {
        ReceivedOn = DateTimeOffset.UtcNow;

        Subject = mimeMessage.Subject;

        var allFrom = mimeMessage.From?.Select(f => f.ToString())?.ToList() ?? [];

        From = string.Join(',', allFrom);

        var allTo = mimeMessage.To?.Select(f => f.ToString())?.ToList() ?? [];

        To = string.Join(',', allTo);

        var allCc = mimeMessage.Cc?.Select(f => f.ToString())?.ToList() ?? [];

        Cc = string.Join(',', allCc);

        var allBcc = mimeMessage.Bcc?.Select(f => f.ToString())?.ToList() ?? [];

        Bcc = string.Join(',', allBcc);

        Importance = mimeMessage.Importance switch
        {
            MessageImportance.Low => "Low",
            MessageImportance.Normal => "Normal",
            MessageImportance.High => "High",
            _ => string.Empty,
        };

        var textContent = mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);
        TextContent = textContent;

        if (mimeMessage.HtmlBody != null)
        {
            var htmlContent = mimeMessage.HtmlBody
                ?? throw new Exception("Could not read message HtmlBody");

            var bodyParts = mimeMessage.BodyParts
                .Where(a => !string.IsNullOrEmpty(a.ContentId))
                .Select(a => new MessageAttachement(a))
                .ToList();

            var realAttachments = mimeMessage.Attachments
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

        if (mimeMessage.Attachments.Any())
        {
            Attachements = mimeMessage.Attachments
                            .Where(a => a.IsAttachment)
                            .Select(a => new MessageAttachement(a))
                            .ToList();

            AttachementsCount = Attachements.Count;
        }
    }

}