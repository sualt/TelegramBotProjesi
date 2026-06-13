// src/TelegramBot.Core/Models/InteractionLog.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TelegramBot.Core.Models;

public class InteractionLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string TelegramId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}