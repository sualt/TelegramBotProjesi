// src/TelegramBot.Infrastructure/MongoDB/MongoDbContext.cs
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using TelegramBot.Core.Models;

namespace TelegramBot.Infrastructure.MongoDB;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoSettings> settings)
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("conventions", pack, t => true);

        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        CreateIndexes();
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<DailyUsage> DailyUsages =>
        _database.GetCollection<DailyUsage>("daily_usages");

    public IMongoCollection<QueryRecord> Queries =>
        _database.GetCollection<QueryRecord>("queries");

    public IMongoCollection<InteractionLog> InteractionLogs =>
        _database.GetCollection<InteractionLog>("interaction_logs");

    private void CreateIndexes()
    {
        var userIndex = Builders<User>.IndexKeys.Ascending(u => u.TelegramId);
        Users.Indexes.CreateOne(new CreateIndexModel<User>(userIndex,
            new CreateIndexOptions { Unique = true }));

        var usageIndex = Builders<DailyUsage>.IndexKeys
            .Ascending(d => d.TelegramId)
            .Ascending(d => d.UsageDate);
        DailyUsages.Indexes.CreateOne(new CreateIndexModel<DailyUsage>(usageIndex,
            new CreateIndexOptions { Unique = true }));

        var queryIndex = Builders<QueryRecord>.IndexKeys.Descending(q => q.QueriedAt);
        Queries.Indexes.CreateOne(new CreateIndexModel<QueryRecord>(queryIndex));
    }
}