using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Net;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;
using System.Net;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp.services;

public class MessageStore : IMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly IncomingEmailValidator _incomingValidator;

    public MessageStore(ILogger<MessageStore> logger, IServiceProvider services, IConfiguration config, IncomingEmailValidator incomingValidator)
    {
        _logger = logger;
        _services = services;
        _config = config;
        _incomingValidator = incomingValidator;
    }

    public Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
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

            _logger.LogInformation("Parsing message & saving data...");
            using var memory = new MemoryStream(newRawMsg.Content);
            using var mimeMessage = MimeMessage.Load(memory, cancellationToken) ?? throw new Exception("Could not parse message.");

            var DkimFailed = CheckDKIM(newRawMsg, mimeMessage);
            var spfStatus = CheckSpf(context, transaction, newRawMsg);
            var isSpam = DkimFailed || spfStatus != SpfVerifyResult.Pass;

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
                return Task.FromResult(SmtpResponse.NoValidRecipientsGiven);
            }

            foreach (var userMailbox in usersMailboxes.Values)
            {
                var newMessage = new Message(mimeMessage)
                {
                    RawMessageId = newRawMsg.Id,
                    UserId = userMailbox.UserId,
                    DkimFailed = DkimFailed,
                    SpfStatus = spfStatus,
                    IsSpam = isSpam
                };

                _dataContext.Messages.Add(newMessage);
                _dataContext.SaveChanges();

                _logger.LogDebug($"Saved message id #{newMessage.Id}.");
            }

            return Task.FromResult(SmtpResponse.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not save incoming message: {0}", ex.Message);
            return Task.FromResult(SmtpResponse.TransactionFailed);
        }
    }

    private SpfVerifyResult CheckSpf(ISessionContext context, IMessageTransaction transaction, RawMessage newRawMsg)
    {
        try
        {
            var from = transaction.From.AsAddress();
            var isSpfCheckEnable = _config.GetValue<bool>("SPF:Enabled") == true;
            if (isSpfCheckEnable)
            {
                var endpoint = (IPEndPoint)context.Properties[EndpointListener.RemoteEndPointKey];
                var ip = endpoint.Address.ToString();
                var domain = transaction.From.Host;
                return _incomingValidator.VerifySpf(ip, domain, from);
            }
            else
            {
                return SpfVerifyResult.None;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
        }

        return SpfVerifyResult.Fail;
    }

    private bool CheckDKIM(RawMessage newRawMsg, MimeMessage mimeMessage)
    {
        try
        {
            var isDkimCheckEnabled = _config.GetValue<bool>("DKIM:Enabled") == true;
            if (isDkimCheckEnabled)
            {
                return !_incomingValidator.VerifyDkim(mimeMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
        }

        return false;
    }
}

