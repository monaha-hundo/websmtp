using MimeKit;

namespace websmtp
{
    public class Message
    {
        public Guid Id { get; set; }
        public byte[] Raw { get; set; }
        public DateTimeOffset ReceivedOn { get; set; } = DateTimeOffset.Now;
        public int Size => Raw.Length;

        public Message(Guid id, byte[] raw)
        {
            Id = id;
            Raw = raw;
            ReceivedOn = DateTimeOffset.UtcNow;

            using var memory = new MemoryStream(Raw);
            var _mimeMessage = MimeMessage.Load(memory);

            Subject = _mimeMessage.Subject;

            var allFrom = _mimeMessage.From?.Select(f => f.ToString()).ToList()
                ?? new List<string>(0);

            From = string.Join(',', allFrom);
            Sender = _mimeMessage.Sender?.Address;

            var allTo = _mimeMessage.To?.Select(f => f.ToString()).ToList()
                ?? new List<string>(0);

            To = string.Join(',', allTo);

            var textContent = _mimeMessage.GetTextBody(MimeKit.Text.TextFormat.Text);
            TextContent = textContent;

            if (_mimeMessage.HtmlBody != null)
            {
                var htmlContent = _mimeMessage.HtmlBody;

                // make resizable, should really be handled with HTTP CSP
                htmlContent = htmlContent?.Replace("</body>", $@"
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

        public string Subject { get; }

        public string From { get; }

        public string Sender { get; }

        public string To { get; }

        public string TextContent { get; }

        public string? HtmlContent { get; }

        public List<MessageAttachement> Attachements { get; } = new();
        public bool Read { get; set; }

    }

    public class MessageAttachement
    {
        public string Filename { get; set; }
        public string MimeType { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ContentId { get; set; }

        public MessageAttachement(MimeEntity mimeEntity)
        {
            MimeType = mimeEntity?.ContentType?.MimeType ?? "application/octet-stream";
            Filename = mimeEntity?.ContentDisposition.FileName
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
    }
}