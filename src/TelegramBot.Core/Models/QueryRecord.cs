// src/TelegramBot.Core/Models/QueryRecord.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TelegramBot.Core.Models;

public class QueryRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string TelegramId { get; set; } = string.Empty;
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public decimal Result { get; set; }
    public bool FromCache { get; set; }
    public DateTime QueriedAt { get; set; } = DateTime.UtcNow;
}