// src/TelegramBot.Infrastructure/MongoDB/MongoSettings.cs
namespace TelegramBot.Infrastructure.MongoDB;

public class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}