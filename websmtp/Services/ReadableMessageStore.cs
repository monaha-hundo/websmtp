using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;

public class ReadableMessageStore : IReadableMessageStore
{
    private readonly ILogger<MessageStore> _logger;
    private readonly IServiceProvider _services;

    public ReadableMessageStore(ILogger<MessageStore> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public ListResult Latest(int page, int perPage, bool onlyNew, bool showTrash, string filter)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();


        _dataContext.ChangeTracker.LazyLoadingEnabled = false;
        _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var total = _dataContext.Messages.Count();
        var newCount = _dataContext.Messages.Count(msg => !msg.Read);
        var deletedCount = _dataContext.Messages.Count(msg => msg.Deleted);

        var query = _dataContext.Messages
            .AsNoTracking()
            .AsSplitQuery();

        if (showTrash)
        {
            query = query.Where(msg => msg.Deleted);
        }
        else
        {
            query = query.Where(msg => !msg.Deleted);
        }

        if (onlyNew)
        {
            query = query.Where(msg => !msg.Read);
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var tokens = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in tokens)
            {
                query = query.Where(msg => msg.Subject.Contains(token) 
                                        || msg.To.Contains(token) 
                                        || msg.From.Contains(token)
                                        || (msg.TextContent != null && msg.TextContent.Contains(token)));
            }
        }

        var messages = query
            .Select(msg => new MessageInfo
            {
                Id = msg.Id,
                AttachementsCount = msg.AttachementsCount,
                From = msg.From,
                Read = msg.Read,
                Deleted = msg.Deleted,
                ReceivedOn = msg.ReceivedOn,
                Subject = msg.Subject,
                To = msg.To,
                Cc = msg.Cc,
                Bcc = msg.Bcc,
                Importance = msg.Importance,
                //Size = msg.Size
            })
            .OrderByDescending(msg => msg.ReceivedOn)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToList();

        return new ListResult
        {
            Count = messages.Count,
            New = newCount,
            Deleted = deletedCount,
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
            return _dataContext.Messages
                .Where(msg => !msg.Deleted)
                .Count(msg => !msg.Read);
        }

        return _dataContext.Messages
            .Where(msg => !msg.Deleted)
            .Count();
    }

    public Message Single(Guid msgId, bool includeRaw = false)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var query = _dataContext.Messages
            .AsNoTracking();

        if (includeRaw)
        {
            query = query
                .Include(msg => msg.Attachements)
                .Include(msg => msg.RawMessage);
        }
        else
        {
            query = query
                .Include(msg => msg.Attachements);
        }

        return query.Single(msg => msg.Id == msgId);
    }

    public void MarkAsRead(Guid msgId)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var msg = _dataContext.Messages.Single(msg => msg.Id == msgId);
        msg.Read = true;
        _dataContext.SaveChanges();
    }

    public void Delete(Guid msgId)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var msg = _dataContext.Messages.Single(msg => msg.Id == msgId);
        msg.Deleted = true;
        _dataContext.SaveChanges();
    }
    public void Undelete(Guid msgId)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var msg = _dataContext.Messages.Single(msg => msg.Id == msgId);
        msg.Deleted = false;
        _dataContext.SaveChanges();
    }
}