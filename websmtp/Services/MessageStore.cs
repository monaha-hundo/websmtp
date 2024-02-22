using Microsoft.EntityFrameworkCore;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.Pages;

public class MessageStore : IMessageStore, IReadableMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;

    public MessageStore(ILogger<MessageStore> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public ListResult Latest(int page, int perPage, bool onlyNew, string filter)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var total = _dataContext.Messages.Count();
        var newCount = _dataContext.Messages.Count(msg => !msg.Read);

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
            foreach (var token in tokens)
            {
                query = query.Where(msg => msg.Subject.Contains(token) || msg.To.Contains(token) || msg.From.Contains(token));
            }
        }

        var messages = query
            .OrderByDescending(msg => msg.ReceivedOn)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(msg => new MessageInfo{
                Id = msg.Id,
                AttachementsCount = msg.AttachementsCount,
                From = msg.From,
                Read = msg.Read,
                ReceivedOn = msg.ReceivedOn,
                Subject = msg.Subject,
                To = msg.To
            })
            .ToList();

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
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        if (onlyNew)
        {
            return _dataContext.Messages.Count(msg => !msg.Read);
        }

        return _dataContext.Messages.Count();
    }

    public Message Single(Guid msgId)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        return _dataContext.Messages
            .AsNoTracking()
            .Include(msg => msg.Attachements)
            .Single(msg => msg.Id == msgId);
    }

    public void MarkAsRead(Guid msgId)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
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