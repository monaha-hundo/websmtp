using Microsoft.EntityFrameworkCore;
using websmtp;
using websmtp.Database;
using websmtp.Database.Models;
using websmtp.Services.Models;

namespace websmtp.services;
public class ReadableMessageStore : IReadableMessageStore
{
    private readonly ILogger<WritableMessageStore> _logger;
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SpamAssassin _sa;


    public ReadableMessageStore(ILogger<WritableMessageStore> logger, IServiceProvider services, IHttpContextAccessor httpContextAccessor, SpamAssassin sa)
    {
        _logger = logger;
        _services = services;
        _httpContextAccessor = httpContextAccessor;
        _sa = sa;
    }


    public StatsResult Stats()
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var userId = _httpContextAccessor.GetUserId();

        _dataContext.ChangeTracker.LazyLoadingEnabled = false;
        _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        if (!_dataContext.Messages.Any())
        {

            return new StatsResult
            {
                Inbox = 0,
                All = 0,
                Favs = 0,
                Spam = 0,
                Trash = 0,
                AllHasNew = false,
                SpamHasNew = false,
                TrashHasNew = false,
            };
        }

        var basicMsgQuery = _dataContext.Messages.Where(msg => msg.UserId == userId);

        var inboxCount = basicMsgQuery.Count(msg => !msg.Sent && !msg.Deleted && !msg.Read && !msg.IsSpam);

        var allMailCount = basicMsgQuery.Count(msg => !msg.Sent && !msg.Deleted && !msg.IsSpam);
        var AllHasNew = inboxCount > 0;

        var spamnCount = basicMsgQuery.Count(msg => !msg.Sent && !msg.Deleted && msg.IsSpam);
        var spamHasNew = basicMsgQuery.Any(msg => !msg.Sent && !msg.Read && !msg.Deleted && msg.IsSpam);

        var trashCount = basicMsgQuery.Count(msg => !msg.Sent && msg.Deleted);
        var trashHasNew = basicMsgQuery.Any(msg => !msg.Sent && msg.Deleted && !msg.Read);

        var favsCount = basicMsgQuery.Count(msg => !msg.Sent && msg.Stared && !msg.Deleted && !msg.IsSpam);

        return new StatsResult
        {
            Inbox = inboxCount,
            All = allMailCount,
            Favs = favsCount,
            Spam = spamnCount,
            Trash = trashCount,
            AllHasNew = AllHasNew,
            SpamHasNew = spamHasNew,
            TrashHasNew = trashHasNew,
        };
    }


    public ListResult List(int page, int perPage, string filter, ListType listType)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.ChangeTracker.LazyLoadingEnabled = false;
        _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var userId = _httpContextAccessor.GetUserId();

        var basicQuery = _dataContext.Messages.Where(msg => msg.UserId == userId)
                            .AsNoTracking()
                            .AsSplitQuery();

        var query = listType switch
        {
            ListType.Inbox => basicQuery.Where(msg => !msg.Sent && !msg.Deleted && !msg.IsSpam && !msg.Read),
            ListType.All => basicQuery.Where(msg => !msg.Sent && !msg.Deleted && !msg.IsSpam),
            ListType.Favorites => basicQuery.Where(msg => msg.Stared),
            ListType.Sent => basicQuery.Where(msg => msg.Sent),
            ListType.Spam => basicQuery.Where(msg => !msg.Sent && msg.IsSpam),
            ListType.Trash => basicQuery.Where(msg => msg.Deleted),
            _ => throw new Exception("Invalid list type")
        };

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
            .Select(msg => new MessageInfo(msg))
            .ToList();

        return new ListResult(messages);
    }

    public long Count(bool onlyNew)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var userId = _httpContextAccessor.GetUserId();

        if (onlyNew)
        {
            return _dataContext.Messages
                .Where(msg => msg.UserId == userId)
                .Where(msg => !msg.Deleted)
                .Count(msg => !msg.Read);
        }

        return _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(msg => !msg.Deleted)
            .Count();
    }

    public Message Single(Guid msgId, bool includeRaw = false)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();

        var query = _dataContext.Messages
            .Where(msg => msg.UserId == userId)
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
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Read, true));
    }
    public void MarkAsUnread(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Read, false));
    }

    public void Delete(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Deleted, true));
    }

    public void Undelete(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Deleted, false));
    }

    public void Star(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Stared, true));

        var msg = _dataContext.Messages.Where(m => msgIds.Contains(m.Id)).ToList();
    }

    public void Unstar(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.Stared, false));
    }

    public void Spam(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.IsSpam, true));

        var msg = _dataContext.Messages.Where(m => msgIds.Contains(m.Id)).ToList();
    }

    public async Task TrainSpam(List<Guid> msgIds, bool isSpam)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        var msgs = _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .Include(m => m.RawMessage)
            .Select(m => new { m.Id, m.RawMessage.Content });

        foreach (var msg in msgs)
        {
            var message = System.Text.Encoding.UTF8.GetString(msg.Content);
            await _sa.Train(message, isSpam);
        }

        if (isSpam)
        {
            Spam(msgIds);
        }
        else
        {
            NotSpam(msgIds);
        }
    }

    public void NotSpam(List<Guid> msgIds)
    {
        using var scope = _services.CreateScope();
        using var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var userId = _httpContextAccessor.GetUserId();
        _dataContext.Messages
            .Where(msg => msg.UserId == userId)
            .Where(m => msgIds.Contains(m.Id))
            .ExecuteUpdate(s => s.SetProperty(m => m.IsSpam, false));
    }
}