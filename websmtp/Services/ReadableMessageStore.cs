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

    public ListResult Latest(int page, int perPage, bool onlyNew, bool showTrash, bool showSpam, bool onlySared, bool showSent, string filter)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        _dataContext.ChangeTracker.LazyLoadingEnabled = false;
        _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        if (!_dataContext.Messages.Any())
        {

            return new ListResult
            {
                Count = 0,
                New = 0,
                Spam = 0,
                Deleted = 0,
                Total = 0,
                Favs = 0,
                AllHasNew = false,
                SpamHasNew = false,
                TrashHasNew = false,
                Messages = []
            };
        }

        var newCount = _dataContext.Messages.Count(msg => !msg.Sent && !msg.Deleted && !msg.Read && !msg.DkimFailed && msg.SpfStatus == SpfVerifyResult.Pass);

        var allMailCount = _dataContext.Messages.Count(msg => !msg.Sent && !msg.Deleted && !msg.DkimFailed && msg.SpfStatus == SpfVerifyResult.Pass);
        var AllHasNew = newCount > 0;

        var spamnCount = _dataContext.Messages.Count(msg => !msg.Sent && !msg.Deleted && (msg.DkimFailed || msg.SpfStatus != SpfVerifyResult.Pass));
        var spamHasNew = _dataContext.Messages.Any(msg => !msg.Sent && !msg.Read && !msg.Deleted && (msg.DkimFailed || msg.SpfStatus != SpfVerifyResult.Pass));

        var trashCount = _dataContext.Messages.Count(msg => !msg.Sent && msg.Deleted );
        var trashHasNew = _dataContext.Messages.Any(msg => !msg.Sent && msg.Deleted);

        var favsCount = _dataContext.Messages.Count(msg => !msg.Sent && msg.Stared && !msg.Deleted && !msg.DkimFailed && msg.SpfStatus == SpfVerifyResult.Pass);

        var query = _dataContext.Messages
            .AsNoTracking()
            .AsSplitQuery();

        if (showSent)
        {
            query = query.Where(msg => msg.Sent);
        }
        else
        {
            query = query.Where(msg => !msg.Sent);
        }

        if (showTrash)
        {
            query = query.Where(msg => msg.Deleted);
        }
        else
        {
            query = query.Where(msg => !msg.Deleted);

            if (!showSpam)
            {
                query = query.Where(msg => !msg.DkimFailed && msg.SpfStatus == SpfVerifyResult.Pass);
            }
            else
            {
                query = query.Where(msg => msg.DkimFailed || msg.SpfStatus != SpfVerifyResult.Pass);
            }

            if (onlyNew)
            {
                query = query.Where(msg => !msg.Read);
            }

            if (onlySared)
            {
                query = query.Where(msg => msg.Stared);
            }
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
            .OrderByDescending(msg => msg.ReceivedOn)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(msg => new MessageInfo
            {
                Id = msg.Id,
                AttachementsCount = msg.AttachementsCount,
                From = msg.From,
                Stared = msg.Stared,
                Read = msg.Read,
                Deleted = msg.Deleted,
                ReceivedOn = msg.ReceivedOn,
                Subject = msg.Subject,
                To = msg.To,
                Cc = msg.Cc,
                Bcc = msg.Bcc,
                Importance = msg.Importance,
            })
            .ToList();

        return new ListResult
        {
            Count = messages.Count,
            New = newCount,
            Spam = spamnCount,
            Deleted = trashCount,
            Total = allMailCount,
            Favs = favsCount,
            AllHasNew = AllHasNew,
            SpamHasNew = spamHasNew,
            TrashHasNew = trashHasNew,
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

    public void MarkAsRead(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Read, true));
    }
    public void MarkAsUnread(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Read, false));
    }

    public void Delete(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Deleted, true));
    }

    public void Undelete(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Deleted, false));
    }

    public void Star(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Stared, true));

        var msg = _dataContext.Messages.Where(m => msgIds.Contains(m.Id)).ToList();
    }

    public void Unstar(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.Messages
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Stared, false));
    }
}