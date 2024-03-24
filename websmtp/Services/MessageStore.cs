using MailKit.Security;
using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Net;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;
using System.Net;
using System.Text.RegularExpressions;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp.services;

public class MessageStore : IMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly SpamAssassin _assassin;

    public MessageStore(ILogger<MessageStore> logger, IServiceProvider services, IConfiguration config, SpamAssassin assassin)
    {
        _logger = logger;
        _services = services;
        _config = config;
        _assassin = assassin;
    }

    public async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            var checkSpam = _config.GetValue<bool>("SpamAssassin:Enabled");
            using var scope = _services.CreateScope();
            using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            _logger.LogInformation("Received message, saving raw data...");
            var raw = buffer.ToArray<byte>();

            var newRawMsg = new RawMessage
            {
                Content = raw
            };

            _dataContext.RawMessages.Add(newRawMsg);
            _dataContext.SaveChanges();
            _logger.LogDebug("Saved raw message id #{}.", newRawMsg.Id);

            byte[] msgBytes;

            if (checkSpam)
            {
                var rawMsg = System.Text.Encoding.UTF8.GetString(raw);
                var processedMsg = await _assassin.Scan(rawMsg);
                msgBytes = System.Text.Encoding.UTF8.GetBytes(processedMsg);
            }
            else
            {
                msgBytes = raw;
            };

            _logger.LogInformation("Parsing message & saving data...");
            using var memory = new MemoryStream(msgBytes);
            using var mimeMessage = MimeMessage.Load(memory, cancellationToken) ?? throw new Exception("Could not parse message.");

            // var saStatus = mimeMessage.Headers["X-Spam-Status"];
            // var saRegEx = new Regex("(score=[0-9.]+) (required=[0-9.]+)");
            // var saRegExResult = saRegEx.Matches(saStatus);
            // var score = double.Parse(saRegExResult[0].Groups[1].Value.Split('=')[1]);
            // var required = double.Parse(saRegExResult[0].Groups[2].Value.Split('=')[1]);
            // var isSpam = score >= required;

            var headers = mimeMessage.Headers.Select(h => $"{h.Field}: {h.Value}").Aggregate((a, b) => $"{a}\r\n{b}");
            headers = headers.Replace("	", " ");

            var isSpam = checkSpam && mimeMessage.Headers.Contains("X-Spam-Flag")
                ? mimeMessage.Headers["X-Spam-Flag"] == "YES"
                : false;

            var usersMailboxes = new Dictionary<int, UserMailbox>();

            foreach (var to in transaction.To)
            {
                var toHost = to.Host;
                var toUser = to.User;

                // specific mailbox
                var userMailbox = _dataContext.Mailboxes.SingleOrDefault(mb => mb.Identity == toUser && mb.Host == toHost);

                if (userMailbox == null)
                {
                    // catch-all@domain mailbox
                    userMailbox = _dataContext.Mailboxes.SingleOrDefault(mb => mb.Identity == "*" && mb.Host == toHost);
                }

                if (userMailbox == null)
                {
                    // CATCH-ALL from ALL domains
                    userMailbox = _dataContext.Mailboxes.SingleOrDefault(mb => mb.Identity == "*" && mb.Host == "*");
                }

                if (userMailbox == null)
                {
                    // no mailbox configured for recipient,
                    // domain catch-all was unconfigured,
                    // catch-all mailbox was unconfigured
                    // reject transaction
                    //return Task.FromResult(SmtpResponse.NoValidRecipientsGiven); // should we?
                    continue;
                }

                if (usersMailboxes.ContainsKey(userMailbox.Id)) continue;

                usersMailboxes.Add(userMailbox.Id, userMailbox);
            }

            if (usersMailboxes.Count == 0)
            {
                return SmtpResponse.NoValidRecipientsGiven;
            }

            foreach (var userMailbox in usersMailboxes.Values)
            {
                var newMessage = new Message(mimeMessage)
                {
                    RawMessageId = newRawMsg.Id,
                    UserId = userMailbox.UserId,
                    IsSpam = isSpam,
                    Headers = headers
                };

                _dataContext.Messages.Add(newMessage);
                _dataContext.SaveChanges();

                _logger.LogDebug($"Saved message id #{newMessage.Id}.");
            }

            return SmtpResponse.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not save incoming message: {0}", ex.Message);
            return SmtpResponse.TransactionFailed;
        }
    }
}

