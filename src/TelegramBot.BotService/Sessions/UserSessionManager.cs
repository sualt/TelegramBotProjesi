// src/TelegramBot.BotService/Sessions/UserSessionManager.cs
// Kullanıcının hangi adımda olduğunu RAM'de tutar

namespace TelegramBot.BotService.Sessions;

public record UserSession
{
    public string Step { get; init; } = "from"; // "from" | "to" | "amount"
    public string? From { get; init; }
    public string? To { get; init; }
}

public class UserSessionManager
{
    private readonly Dictionary<long, UserSession> _sessions = new();

    public void SetSession(long telegramId, UserSession session) =>
        _sessions[telegramId] = session;

    public UserSession? GetSession(long telegramId) =>
        _sessions.TryGetValue(telegramId, out var session) ? session : null;

    public void ClearSession(long telegramId) =>
        _sessions.Remove(telegramId);
}