using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.services;

namespace MyApp.Namespace
{
    public partial class NewMessageModel : PageModel
    {
        private readonly Regex emailWithNameRegEx = EmailWithNameRegEx();

        private readonly ILogger<SendMailService> _logger;
        private readonly DataContext _data;

        readonly SendMailService _sendMail;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [FromQuery] public string? InitialTo { get; set; }
        [BindProperty] public int IdentityId { get; set; }
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

        public List<UserIdentity> Identities { get; set; } = [];


        public NewMessageModel(SendMailService sendMail, ILogger<SendMailService> logger, DataContext data, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _sendMail = sendMail;
            _data = data;
            _httpContextAccessor = httpContextAccessor;
        }
        private int GetUserGuid()
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User
                    ?? throw new NullReferenceException("could not find user in HttpContext");

                var nameId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(nameId))
                {
                    throw new Exception("NameIdentifier claim not found in user.");
                }

                return int.Parse(nameId);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get user guid: ", ex);
            }
        }

        public void OnGet()
        {
            if (DebugSent == true)
            {
                Sent = !DebugSentError ?? true;
            }

            var userId = GetUserGuid();
            var user = _data.Users.Include(u => u.Identities).Single(u => u.Id == userId);
            Identities = [.. user.Identities];
        }
        public void OnPost()
        {
            TraceId = Request.HttpContext.TraceIdentifier;

            try
            {
                var userId = GetUserGuid();
                var user = _data.Users.Include(u => u.Identities).Single(u => u.Id == userId);
                var identity = user.Identities.Single(i=>i.Id == IdentityId);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(identity.Email, identity.DisplayName)
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

                using var transaction = _data.Database.BeginTransaction();
                using var memory = new MemoryStream();
                mimeMessage.WriteTo(FormatOptions.Default, memory);
                var rawSentMessage = new RawMessage()
                {
                    Content = memory.ToArray()
                };
                _data.RawMessages.Add(rawSentMessage);
                _data.SaveChanges();

                var sentMessage = new Message(mimeMessage)
                {
                    RawMessageId = rawSentMessage.Id,
                    UserId = userId,
                    Sent = true
                };
                _data.Messages.Add(sentMessage);
                _data.SaveChanges();

                _sendMail.SendMail(mimeMessage);
                Sent = true;
                transaction.Commit();
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
