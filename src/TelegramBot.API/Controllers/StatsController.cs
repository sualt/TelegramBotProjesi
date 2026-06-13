// src/TelegramBot.API/Controllers/StatsController.cs
using Microsoft.AspNetCore.Mvc;
using TelegramBot.Infrastructure.MongoDB;
using MongoDB.Driver;
using TelegramBot.Core.Models;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly MongoDbContext _context;

    public StatsController(MongoDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var stats = new
        {
            TotalUsers = await _context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty),
            BlockedUsers = await _context.Users.CountDocumentsAsync(
                Builders<User>.Filter.Eq(u => u.IsBlocked, true)),
            TotalQueries = await _context.Queries.CountDocumentsAsync(
                FilterDefinition<QueryRecord>.Empty),
            QueriestoDay = await _context.Queries.CountDocumentsAsync(
                Builders<QueryRecord>.Filter.Gte(q => q.QueriedAt, DateTime.UtcNow.Date)),
            PopularPairs = await _context.Queries
                .Aggregate()
                .Group(q => new { q.FromCurrency, q.ToCurrency }, g => new
                {
                    Pair = g.Key.FromCurrency + "/" + g.Key.ToCurrency,
                    Count = g.Count()
                })
                .SortByDescending(x => x.Count)
                .Limit(5)
                .ToListAsync()
        };

        return Ok(stats);
    }

    [HttpGet("interactions")]
    public async Task<IActionResult> GetInteractions()
    {
        var logs = await _context.InteractionLogs
            .Find(FilterDefinition<InteractionLog>.Empty)
            .SortByDescending(l => l.CreatedAt)
            .Limit(50)
            .ToListAsync();

        return Ok(logs);
    }
}