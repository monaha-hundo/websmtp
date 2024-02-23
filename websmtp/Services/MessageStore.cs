using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;
using websmtp.Database;
using websmtp.Database.Models;

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

            _logger.LogInformation("Received message, parsing & saving...");
            var newMessage = new Message(buffer);

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