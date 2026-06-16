// src/TelegramBot.Core/Models/User.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TelegramBot.Core.Models;

public enum SubscriptionPlan { Free, Pro, Admin }

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string TelegramId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsBlocked { get; set; } = false;
    public int DailyLimit { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    // --- YENİ: Üyelik ---
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTime? PlanExpiresAt { get; set; }
    public bool IsPlanActive => Plan == SubscriptionPlan.Free ||
                                Plan == SubscriptionPlan.Admin ||
                                (PlanExpiresAt.HasValue && PlanExpiresAt > DateTime.UtcNow);
}