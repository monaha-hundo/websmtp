using Microsoft.EntityFrameworkCore;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp.services;

public class WritableMessageStore : IMessageStore
{
    private readonly ILogger<WritableMessageStore> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly SpamAssassin _assassin;

    public WritableMessageStore(ILogger<WritableMessageStore> logger, IServiceProvider services, IConfiguration config, SpamAssassin assassin)
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
            using var scope = _services.CreateScope();
            using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            _logger.LogInformation($"Receiving email, checking destinations.");

            var usersMailboxes = new Dictionary<int, UserMailbox>();

            var allMailboxes = await _dataContext.Mailboxes.ToListAsync(cancellationToken);

            foreach (var to in transaction.To)
            {
                var toHost = to.Host;
                var toUser = to.User;

                _logger.LogTrace($"No specific mailbox found, trying '*@{toHost}'.");
                // specific mailbox
                var userMailbox = allMailboxes.SingleOrDefault(mb => mb.Identity == toUser && mb.Host == toHost);

                if (userMailbox == null)
                {
                    _logger.LogTrace($"No specific mailbox found, trying '*@{toHost}'.");
                    // catch-all@domain mailbox
                    userMailbox = allMailboxes.SingleOrDefault(mb => mb.Identity == "*" && mb.Host == toHost);
                }

                if (userMailbox == null)
                {
                    _logger.LogTrace($"No *@{toHost} mailbox found, trying '*@*'");
                    // CATCH-ALL from ALL domains
                    userMailbox = allMailboxes.SingleOrDefault(mb => mb.Identity == "*" && mb.Host == "*");
                }

                if (userMailbox == null)
                {
                    _logger.LogTrace($"No *@* mailbox found, '{toUser}@{toHost}' bogus destination.");
                    continue;
                }

                if (usersMailboxes.ContainsKey(userMailbox.Id)) continue;

                usersMailboxes.Add(userMailbox.Id, userMailbox);
            }

            if (usersMailboxes.Count == 0)
            {
                _logger.LogTrace($"No mailbox found for any destinations, bogus message.");
                return SmtpResponse.NoValidRecipientsGiven;
            }

            _logger.LogInformation($"Found {usersMailboxes.Count} destination(s), saving raw data...");
            var raw = buffer.ToArray();

            var newRawMsg = new RawMessage
            {
                Content = raw
            };

            _dataContext.RawMessages.Add(newRawMsg);
            await _dataContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug($"Saved raw message id #{newRawMsg.Id}.");

            byte[] msgBytes;

            var checkSpam = _config.GetValue<bool>("SpamAssassin:Enabled");
            if (checkSpam)
            {
                var rawMsg = System.Text.Encoding.UTF8.GetString(raw);
                var processedMsg = await _assassin.ScanAsync(rawMsg);
                msgBytes = System.Text.Encoding.UTF8.GetBytes(processedMsg);
            }
            else
            {
                msgBytes = raw;
            };

            _logger.LogInformation($"Parsing message & saving data...");
            using var memory = new MemoryStream(msgBytes);
            using var mimeMessage = await MimeMessage.LoadAsync(memory, cancellationToken) ?? throw new Exception("Could not parse message.");

            var isSpam = checkSpam && mimeMessage.Headers.Contains("X-Spam-Flag")
                && mimeMessage.Headers["X-Spam-Flag"] == "YES";

            var headers = mimeMessage.Headers.Select(h => $"{h.Field}: {h.Value}").Aggregate((a, b) => $"{a}\r\n{b}");
            headers = headers.Replace("	", " ");

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
                await _dataContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug($"Saved message id #{newMessage.Id}.");
            }

            return SmtpResponse.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Could not save incoming message: {ex.Message}");
            return SmtpResponse.TransactionFailed;
        }
    }
}

