using System.Buffers;
using System.Collections.Frozen;
using MimeKit;

namespace websmtp
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.Empty;
        public byte[] Raw { get; set; } = [];
        public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.Now;
        public int Size => Raw.Length;

        public string Subject { get; set; } = string.Empty;

        public string From { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;

        public string Sender { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public string TextContent { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }

        public List<MessageAttachement> Attachements { get; set; } = [];

        public bool Read { get; set; }

        public MimeMessage? Reply { get; set; }

        public bool Replied => Reply != null;

        public Message()
        {

        }

        public Message(Guid id, ReadOnlySequence<byte> buffer)
        {
            Id = id;
            Raw = buffer.ToArray<byte>();
            ReceivedOn = DateTimeOffset.UtcNow;

            using var memory = new MemoryStream(Raw);
            using var _mimeMessage = MimeMessage.Load(memory) ?? throw new Exception("Could not parse message.");

            Subject = _mimeMessage.Subject;

            var allFrom = _mimeMessage.From?.Select(f => f.ToString()).ToList()
                ?? new List<string>(0);

            From = string.Join(',', allFrom);
            Sender = _mimeMessage.Sender?.Address ?? From;
            SenderAddress = _mimeMessage.Sender?.Address ?? _mimeMessage.From.First().ToString(true);
            SenderName = _mimeMessage.Sender?.Name ?? _mimeMessage.From.First().Name;

            var allTo = _mimeMessage.To?.Select(f => f.ToString()).ToList()
                ?? new List<string>(0);

            To = string.Join(',', allTo);

            var textContent = _mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);
            TextContent = textContent;

            if (_mimeMessage.HtmlBody != null)
            {
                var htmlContent = _mimeMessage.HtmlBody
                    ?? throw new Exception("Could not read message HtmlBody");

                // make resizable, should really be handled with HTTP CSP
                htmlContent = htmlContent.Replace("</body>", $@"
                         <script>
                            setInterval(()=>window.parent.postMessage({{ ""type"": ""frame-resized"", ""value"": document.documentElement.clientHeight  }}, '*'), 100);
                         </script>
                         </body>
                    ");

                var bodyParts = _mimeMessage.BodyParts
                    .Where(a => !string.IsNullOrEmpty(a.ContentId))
                    .Select(a => new MessageAttachement(a))
                    .ToList();

                var realAttachments = _mimeMessage.Attachments
                    //.Where(a => a.IsAttachment)
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

            if (_mimeMessage.Attachments.Count() > 0)
            {
                Attachements = _mimeMessage.Attachments
                                .Where(a => a.IsAttachment)
                                .Select(a => new MessageAttachement(a))
                                .ToList();
            }
        }

    }

    public class MessageAttachement
    {
        public string Filename { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;

        public MessageAttachement(MimeEntity mimeEntity)
        {
            MimeType = mimeEntity.ContentType.MimeType ?? "application/octet-stream";
            Filename = mimeEntity.ContentType.Name
                ?? mimeEntity.ContentDisposition.FileName
                ?? mimeEntity.ContentId;
            ContentId = mimeEntity.ContentId;
            using var tempMemory = new MemoryStream();
            var wat = new FormatOptions();
            mimeEntity.WriteTo(wat, tempMemory, true);
            var mimeBytes = tempMemory.ToArray();
            //var mimeBase64 = Convert.ToBase64String(mimeBytes);
            var phase2Bytes = System.Text.Encoding.Default.GetString(mimeBytes);
            Content = phase2Bytes;
        }

        public MessageAttachement()
        {
        }
    }
}