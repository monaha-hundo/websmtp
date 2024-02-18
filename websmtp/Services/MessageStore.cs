using System.Buffers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IConfiguration _config;
    private readonly DataContext _dataContext;

    public MessageStore(ILogger<MessageStore> logger, IConfiguration config, DataContext dataContext)
    {
        _logger = logger;
        _config = config;
        _dataContext = dataContext;
    }

    public ListResult Latest(int page, int perPage, bool onlyNew, string filter)
    {
        var query = _dataContext.Messages
            .AsNoTracking()
            .AsSplitQuery();

        if (onlyNew)
        {
            query = query.Where(msg => !msg.Read);
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var tokens = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(msg => tokens.Any(t => msg.To.Contains(t))
                || tokens.Any(t => msg.From.Contains(t))
                || tokens.Any(t => msg.Subject.Contains(t)));
        }

        var total = query.Count();
        var newCount = query.Count(msg => !msg.Read);
        var messages = query.Skip((page - 1) * perPage).Take(perPage).ToList();

        return new ListResult
        {
            Count = messages.Count,
            New = newCount,
            Total = total,
            Messages = messages
        };
    }

    public long Count(bool onlyNew)
    {
        if (onlyNew)
        {
            return _dataContext.Messages.Count(msg => !msg.Read);
        }

        return _dataContext.Messages.Count();
    }

    public Message Single(Guid msgId)
    {
        return _dataContext.Messages
            .AsNoTracking()
            .Single(msg => msg.Id == msgId);
    }

    public void MarkAsRead(Guid msgId)
    {
        var msg = _dataContext.Messages.Single(msg => msg.Id == msgId);
        msg.Read = true;
        _dataContext.SaveChanges();
    }

    public Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
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
            throw;
        }
    }
}