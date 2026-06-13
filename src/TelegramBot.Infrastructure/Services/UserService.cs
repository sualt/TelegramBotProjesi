using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TelegramBot.Core.Models;
using TelegramBot.Infrastructure.MongoDB;

namespace TelegramBot.Infrastructure.Services;

public class UserService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(MongoDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(long telegramId, string? username,
        string? firstName, string? lastName)
    {
        var id = telegramId.ToString();
        var filter = Builders<User>.Filter.Eq(u => u.TelegramId, id);
        var existing = await _context.Users.Find(filter).FirstOrDefaultAsync();

        if (existing != null)
        {
            var update = Builders<User>.Update
                .Set(u => u.LastActive, DateTime.UtcNow)
                .Set(u => u.Username, username)
                .Set(u => u.FirstName, firstName);

            await _context.Users.UpdateOneAsync(filter, update);
            return existing;
        }

        var newUser = new User
        {
            TelegramId = id,
            Username = username,
            FirstName = firstName,
            LastName = lastName
        };

        await _context.Users.InsertOneAsync(newUser);
        _logger.LogInformation("Yeni kullanıcı: {TelegramId}", id);
        return newUser;
    }

    public async Task<bool> IsBlockedAsync(long telegramId)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.TelegramId, telegramId.ToString()),
            Builders<User>.Filter.Eq(u => u.IsBlocked, true)
        );
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<(bool Allowed, int Remaining, int Used)> CheckAndUseLimitAsync(long telegramId)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var id = telegramId.ToString();
        const int limit = 10;

        var filter = Builders<DailyUsage>.Filter.And(
            Builders<DailyUsage>.Filter.Eq(d => d.TelegramId, id),
            Builders<DailyUsage>.Filter.Eq(d => d.UsageDate, today)
        );

        var usage = await _context.DailyUsages.Find(filter).FirstOrDefaultAsync();
        var currentCount = usage?.Count ?? 0;

        if (currentCount >= limit)
            return (false, 0, currentCount);

        if (usage == null)
        {
            await _context.DailyUsages.InsertOneAsync(new DailyUsage
            {
                TelegramId = id,
                UsageDate = today,
                Count = 1
            });
        }
        else
        {
            var update = Builders<DailyUsage>.Update.Inc(d => d.Count, 1);
            await _context.DailyUsages.UpdateOneAsync(filter, update);
        }

        return (true, limit - currentCount - 1, currentCount + 1);
    }

    public async Task SaveQueryAsync(string telegramId, string from, string to,
        decimal amount, decimal rate, decimal result, bool fromCache)
    {
        await _context.Queries.InsertOneAsync(new QueryRecord
        {
            TelegramId = telegramId,
            FromCurrency = from,
            ToCurrency = to,
            Amount = amount,
            Rate = rate,
            Result = result,
            FromCache = fromCache
        });
    }

    public async Task LogInteractionAsync(string telegramId, string action,
        Dictionary<string, object>? details = null)
    {
        return;
    }

    public async Task<List<User>> GetAllUsersAsync(int page = 1, int limit = 20, bool? blocked = null)
    {
        var filter = blocked.HasValue
            ? Builders<User>.Filter.Eq(u => u.IsBlocked, blocked.Value)
            : Builders<User>.Filter.Empty;

        return await _context.Users.Find(filter)
            .SortByDescending(u => u.LastActive)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> SetBlockedAsync(string telegramId, bool isBlocked)
    {
        var filter = Builders<User>.Filter.Eq(u => u.TelegramId, telegramId);
        var update = Builders<User>.Update.Set(u => u.IsBlocked, isBlocked);
        var result = await _context.Users.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<int> GetTodayUsageAsync(string telegramId, string today)
    {
        var filter = Builders<DailyUsage>.Filter.And(
            Builders<DailyUsage>.Filter.Eq(d => d.TelegramId, telegramId),
            Builders<DailyUsage>.Filter.Eq(d => d.UsageDate, today)
        );
        var usage = await _context.DailyUsages.Find(filter).FirstOrDefaultAsync();
        return usage?.Count ?? 0;
    }

    public async Task<long> GetTotalQueriesAsync(string telegramId)
    {
        var filter = Builders<QueryRecord>.Filter.Eq(q => q.TelegramId, telegramId);
        return await _context.Queries.CountDocumentsAsync(filter);
    }

    public async Task<List<QueryRecord>> GetRecentQueriesAsync(string telegramId, int count = 5)
    {
        var filter = Builders<QueryRecord>.Filter.Eq(q => q.TelegramId, telegramId);
        return await _context.Queries
            .Find(filter)
            .SortByDescending(q => q.QueriedAt)
            .Limit(count)
            .ToListAsync();
    }
}