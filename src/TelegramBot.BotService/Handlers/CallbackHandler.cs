using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.BotService.Sessions;
using TelegramBot.Infrastructure.Services;

namespace TelegramBot.BotService.Handlers;

public class CallbackHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserSessionManager _sessions;
    private readonly MessageHandler _messageHandler;
    private readonly UserService _userService;

    public CallbackHandler(
        ITelegramBotClient bot,
        UserSessionManager sessions,
        MessageHandler messageHandler,
        UserService userService)
    {
        _bot = bot;
        _sessions = sessions;
        _messageHandler = messageHandler;
        _userService = userService;
    }

    public async Task HandleAsync(CallbackQuery query, CancellationToken ct)
    {
        await _bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        var telegramId = query.From.Id;
        var data = query.Data ?? string.Empty;
        var chatId = query.Message!.Chat.Id;
        var messageId = query.Message.MessageId;

        await _userService.LogInteractionAsync(telegramId.ToString(), "button_click",
            new Dictionary<string, object> { { "button", data } });

        if (data.StartsWith("from_"))
        {
            var currency = data["from_".Length..];
            _sessions.SetSession(telegramId, new UserSession { Step = "to", From = currency });

            await _bot.EditMessageText(chatId, messageId,
                $"✅ Kaynak: *{currency}*\n\nHedef para birimini seç:",
                parseMode: ParseMode.Markdown,
                replyMarkup: MessageHandler.GetCurrencyInlineKeyboard("to", currency),
                cancellationToken: ct);
            return;
        }

        if (data.StartsWith("to_"))
        {
            var currency = data["to_".Length..];
            var session = _sessions.GetSession(telegramId);

            if (session == null)
            {
                await _bot.SendMessage(chatId,
                    "❌ Oturum doldu. /convert ile yeniden başla.",
                    cancellationToken: ct);
                return;
            }

            _sessions.SetSession(telegramId, session with { Step = "amount", To = currency });

            await _bot.EditMessageText(chatId, messageId,
                $"✅ {session.From} → {currency}\n\nMiktarı yaz (sadece sayı):",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
            return;
        }

        if (data.StartsWith("quick_"))
        {
            var parts = data.Split('_');
            if (parts.Length == 3)
                await _messageHandler.ProcessConversionAsync(chatId, telegramId, parts[1], parts[2], 1, ct);
        }
    }
}