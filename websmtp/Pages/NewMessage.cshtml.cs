using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using websmtp;

namespace MyApp.Namespace
{
    public class NewMessageModel : PageModel
    {
        private readonly ILogger<SendMailService> _logger;

        public NewMessageModel(SendMailService sendMail, ILogger<SendMailService> logger)
        {
            _logger = logger;
            _sendMail = sendMail;
        }

        readonly SendMailService _sendMail;

        [FromQuery] public string? InitialTo { get; set; }
        [BindProperty] public string From { get; set; } = string.Empty;
        [BindProperty] public string To { get; set; } = string.Empty;
        [BindProperty] public string Cc { get; set; } = string.Empty;
        [BindProperty] public string Bcc { get; set; } = string.Empty;
        [BindProperty] public string Subject { get; set; } = string.Empty;
        [BindProperty] public bool Html { get; set; }
        [BindProperty] public string Body { get; set; } = string.Empty;
        public bool? Sent { get; set; }

        [FromQuery] public bool? DebugSent { get; set; }
        [FromQuery] public bool? DebugSentError { get; set; }

        public void OnGet()
        {
            if (DebugSent == true)
            {
                Sent = !DebugSentError ?? true;
            }
        }
        public void OnPost()
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(From)
            };

            if (!string.IsNullOrEmpty(To))
            {
                var toList = To.Split(',').ToList();
                toList.ForEach(to => mailMessage.To.Add(to));
            }

            if (!string.IsNullOrEmpty(Cc))
            {
                var ccList = Cc.Split(',').ToList();
                ccList.ForEach(cc => mailMessage.To.Add(cc));
            }

            if (!string.IsNullOrEmpty(Bcc))
            {
                var bccList = Bcc.Split(',').ToList();
                bccList.ForEach(bcc => mailMessage.To.Add(bcc));
            }

            mailMessage.IsBodyHtml = Html;
            mailMessage.Body = Body;
            mailMessage.Subject = Subject;

            var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

            try
            {
                _sendMail.SendMail(mimeMessage);
                Sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Sending mail failed: {}.", ex.Message);
                Sent = false;
            }
        }
    }
}
