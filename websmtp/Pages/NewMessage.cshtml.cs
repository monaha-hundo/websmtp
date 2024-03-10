using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using websmtp;

namespace MyApp.Namespace
{
    public partial class NewMessageModel : PageModel
    {
        private readonly Regex emailWithNameRegEx = EmailWithNameRegEx();

        private readonly ILogger<SendMailService> _logger;

        public NewMessageModel(SendMailService sendMail, ILogger<SendMailService> logger)
        {
            _logger = logger;
            _sendMail = sendMail;
        }

        readonly SendMailService _sendMail;

        [FromQuery] public string? InitialTo { get; set; }
        [BindProperty] public string FromEmail { get; set; } = string.Empty;
        [BindProperty] public string FromName { get; set; } = string.Empty;
        [BindProperty] public string To { get; set; } = string.Empty;
        [BindProperty] public string Cc { get; set; } = string.Empty;
        [BindProperty] public string Bcc { get; set; } = string.Empty;
        [BindProperty] public string Subject { get; set; } = string.Empty;
        [BindProperty] public bool Html { get; set; }
        [BindProperty] public string Body { get; set; } = string.Empty;
        public bool? Sent { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TraceId { get; set; }

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
            TraceId = Request.HttpContext.TraceIdentifier;

            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(FromEmail, FromName)
                };

                if (!string.IsNullOrEmpty(To))
                {
                    var toList = To.Split(',').ToList();
                    toList.ForEach(to =>
                    {
                        if (emailWithNameRegEx.IsMatch(to))
                        {
                            var matches = emailWithNameRegEx.Matches(to);
                            var name = matches[0].Groups[1].Value;
                            var email = matches[0].Groups[2].Value;
                            mailMessage.To.Add(new MailAddress(email, name));
                        }
                        else
                        {
                            mailMessage.To.Add(to);
                        }
                    });
                }

                if (!string.IsNullOrEmpty(Cc))
                {
                    var ccList = Cc.Split(',').ToList();
                    ccList.ForEach(cc =>
                    {
                        if (emailWithNameRegEx.IsMatch(cc))
                        {
                            var matches = emailWithNameRegEx.Matches(cc);
                            var name = matches[0].Groups[1].Value;
                            var email = matches[0].Groups[2].Value;
                            mailMessage.CC.Add(new MailAddress(email, name));
                        }
                        else
                        {
                            mailMessage.CC.Add(cc);
                        }
                    });
                }

                if (!string.IsNullOrEmpty(Bcc))
                {
                    var bccList = Bcc.Split(',').ToList();
                    bccList.ForEach(bcc =>
                    {
                        if (emailWithNameRegEx.IsMatch(bcc))
                        {
                            var matches = emailWithNameRegEx.Matches(bcc);
                            var name = matches[0].Groups[1].Value;
                            var email = matches[0].Groups[2].Value;
                            mailMessage.Bcc.Add(new MailAddress(email, name));
                        }
                        else
                        {
                            mailMessage.Bcc.Add(bcc);
                        }
                    });
                }

                mailMessage.IsBodyHtml = Html;
                mailMessage.Body = Body;
                mailMessage.Subject = Subject;

                var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

                //_sendMail.SendMail(mimeMessage);
                Sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Sending mail failed: {}.", ex.Message);
                Sent = false;
                ErrorMessage = ex.Message;
            }
        }

        [GeneratedRegex(@"""(.+)"" <(.+)>")]
        private static partial Regex EmailWithNameRegEx();
    }
}
