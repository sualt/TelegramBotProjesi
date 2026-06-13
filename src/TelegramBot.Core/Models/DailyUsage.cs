// src/TelegramBot.Core/Models/DailyUsage.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TelegramBot.Core.Models;

public class DailyUsage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string TelegramId { get; set; } = string.Empty;
    public string UsageDate { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
}