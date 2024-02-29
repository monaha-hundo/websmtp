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

namespace websmtp;

public class MessageStore : IMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;

    public MessageStore(ILogger<MessageStore> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
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
            _logger.LogDebug($"Saved raw message id #{newRawMsg.Id}.");

            _logger.LogInformation("Parsing message & saving data...");
            using var memory = new MemoryStream(raw);
            using var mimeMessage = MimeMessage.Load(memory) ?? throw new Exception("Could not parse message.");

            var newMessage = new Message(mimeMessage)
            {
                RawMessageId = newRawMsg.Id
            };

            try
            {
                newMessage.DkimFailed = !IncomingEmailValidator.VerifyDkim(mimeMessage);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
            }

            try
            {
                var endpoint = (IPEndPoint)context.Properties[EndpointListener.RemoteEndPointKey];
                var ip = endpoint.Address.ToString();
                var from = transaction.From.AsAddress();
                var domain = transaction.From.Host;
                newMessage.SpfStatus = IncomingEmailValidator.VerifySpf(ip, domain, from);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Could not validate DKIM signature on incoming message raw_id# {0}: {1}", newRawMsg.Id, ex.Message);
            }


            _dataContext.Messages.Add(newMessage);
            _dataContext.SaveChanges();

            _logger.LogDebug($"Saved message id #{newMessage.Id}.");

            return Task.FromResult(SmtpResponse.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Could not save incoming message: {0}", ex.Message);
            return Task.FromResult(SmtpResponse.TransactionFailed);
        }
    }
}

