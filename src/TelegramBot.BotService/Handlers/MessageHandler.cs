using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.BotService.Sessions;
using TelegramBot.Infrastructure.Services;

namespace TelegramBot.BotService.Handlers;

public class MessageHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserService _userService;
    private readonly ExchangeService _exchangeService;
    private readonly UserSessionManager _sessions;

    public MessageHandler(
        ITelegramBotClient bot,
        UserService userService,
        ExchangeService exchangeService,
        UserSessionManager sessions)
    {
        _bot = bot;
        _userService = userService;
        _exchangeService = exchangeService;
        _sessions = sessions;
    }

    public async Task HandleAsync(Message message, CancellationToken ct)
    {
        if (message.From == null || message.Text == null) return;

        var from = message.From;
        var telegramId = from.Id;
        var text = message.Text;
        var chatId = message.Chat.Id;

        await _userService.GetOrCreateUserAsync(telegramId, from.Username, from.FirstName, from.LastName);

        if (await _userService.IsBlockedAsync(telegramId))
        {
            await _bot.SendTextMessageAsync(chatId,
                "🚫 Hesabın engellenmiştir. @admin ile iletişime geçin.",
                cancellationToken: ct);
            await _userService.LogInteractionAsync(telegramId.ToString(), "blocked_attempt");
            return;
        }

        var session = _sessions.GetSession(telegramId);
        if (session != null && session.Step == "amount" && !text.StartsWith("/"))
        {
            if (decimal.TryParse(text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount) && amount > 0)
            {
                await ProcessConversionAsync(chatId, telegramId, session.From!, session.To!, amount, ct);
                _sessions.ClearSession(telegramId);
            }
            else
            {
                await _bot.SendTextMessageAsync(chatId,
                    "❌ Geçerli bir sayı gir (örn: 100)",
                    cancellationToken: ct);
            }
            return;
        }

        switch (text)
        {
            case "/start":
                await HandleStartAsync(chatId, from, ct);
                break;
            case "/help":
            case "❓ Yardım":
                await HandleHelpAsync(chatId, telegramId, ct);
                break;
            case "/menu":
                await HandleMenuAsync(chatId, telegramId, ct);
                break;
            case "/convert":
            case "💱 Döviz Çevir":
                await HandleConvertAsync(chatId, telegramId, ct);
                break;
            case "/dashboard":
            case "📊 Dashboard'um":
                await HandleDashboardAsync(chatId, telegramId, ct);
                break;
            case "📈 Popüler Kurlar":
                await HandlePopularRatesAsync(chatId, telegramId, ct);
                break;
        }
    }

    private async Task HandleStartAsync(long chatId, User from, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(from.Id.ToString(), "start");

        var text = $"👋 Merhaba *{from.FirstName}*!\n\n" +
                   "💱 *Döviz Çevirici Bot*'a hoş geldin!\n\n" +
                   "📋 Neler yapabilirsin:\n" +
                   "• Anlık döviz kuru sorgulama\n" +
                   "• Günlük 10 sorgu hakkı\n\n" +
                   "/menu → Ana menüyü aç";

        await _bot.SendTextMessageAsync(chatId, text,
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task HandleHelpAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "help");

        var text = "📖 *Yardım Menüsü*\n\n" +
                   "*Komutlar:*\n" +
                   "/start → Botu başlat\n" +
                   "/menu → Ana menü\n" +
                   "/convert → Döviz çevirici\n" +
                   "/dashboard → Kendi istatistiklerin\n" +
                   "/help → Bu yardım menüsü\n\n" +
                   "*Nasıl çalışır?*\n" +
                   "1. /convert yaz\n" +
                   "2. Kaynak para birimi seç\n" +
                   "3. Hedef para birimi seç\n" +
                   "4. Miktarı gir\n" +
                   "5. Sonucu al ✅\n\n" +
                   "*Limitler:*\n" +
                   "• Günlük 10 sorgu hakkı\n" +
                   "• Kurlar 5 dakikada bir önbelleklenir\n" +
                   "• Gece yarısı haklar sıfırlanır 🌙";

        await _bot.SendTextMessageAsync(chatId, text,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandleMenuAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "menu_open");
        await _bot.SendTextMessageAsync(chatId, "📋 Ana Menü:",
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task HandleConvertAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "convert_start");
        _sessions.SetSession(telegramId, new UserSession { Step = "from" });

        await _bot.SendTextMessageAsync(chatId, "💱 Kaynak para birimini seç:",
            replyMarkup: GetCurrencyInlineKeyboard("from"),
            cancellationToken: ct);
    }

    private async Task HandleDashboardAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "dashboard_view");

        var id = telegramId.ToString();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var todayUsage = await _userService.GetTodayUsageAsync(id, today);
        var totalQueries = await _userService.GetTotalQueriesAsync(id);
        var recentQueries = await _userService.GetRecentQueriesAsync(id, 5);

        var remaining = 10 - todayUsage;
        var bars = string.Concat(Enumerable.Repeat("🟢", Math.Min(remaining, 5)));

        var text = $"📊 *Dashboard'um*\n\n" +
                   $"🎯 Bugün: {todayUsage}/10 kullandım\n" +
                   $"✅ Kalan: {remaining} {bars}\n\n" +
                   $"📈 Toplam: {totalQueries} sorgu\n\n" +
                   "🕐 *Son Sorgular:*\n";

        if (!recentQueries.Any())
            text += "Henüz sorgu yapılmadı.";
        else
            foreach (var q in recentQueries)
                text += $"\n• {q.FromCurrency}→{q.ToCurrency}: {q.Rate:F4}";

        await _bot.SendTextMessageAsync(chatId, text,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandlePopularRatesAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "popular_rates");
        await _bot.SendTextMessageAsync(chatId, "⏳ Kurlar yükleniyor...", cancellationToken: ct);

        var pairs = new[] { ("USD", "TRY"), ("EUR", "TRY"), ("GBP", "TRY"), ("USD", "EUR") };
        var text = "📈 *Popüler Kurlar*\n\n";

        foreach (var (f, t) in pairs)
        {
            try
            {
                var (rate, fromCache) = await _exchangeService.GetRateAsync(f, t);
                text += $"• {f}/{t}: `{rate:F4}` {(fromCache ? "📦" : "🌐")}\n";
            }
            catch { text += $"• {f}/{t}: Hata\n"; }
        }

        text += "\n_5 dakikada bir güncellenir_";
        await _bot.SendTextMessageAsync(chatId, text,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    public async Task ProcessConversionAsync(long chatId, long telegramId,
        string from, string to, decimal amount, CancellationToken ct)
    {
        var limitResult = await _userService.CheckAndUseLimitAsync(telegramId);

        if (!limitResult.Allowed)
        {
            await _bot.SendTextMessageAsync(chatId,
                "⛔ Günlük 10 sorgu hakkını tükettiniz!\n🌙 Gece yarısı sıfırlanacak.",
                cancellationToken: ct);
            return;
        }

        await _bot.SendTextMessageAsync(chatId, "⏳ Kur alınıyor...", cancellationToken: ct);

        try
        {
            var (rate, fromCache) = await _exchangeService.GetRateAsync(from, to);
            var result = amount * rate;

            await _userService.SaveQueryAsync(telegramId.ToString(), from, to, amount, rate, result, fromCache);

            var text = $"💱 *Döviz Sonucu*\n\n" +
                       $"{amount} *{from}* = *{result:F2} {to}*\n\n" +
                       $"📊 Kur: 1 {from} = {rate:F4} {to}\n" +
                       $"{(fromCache ? "📦 Önbellekten" : "🌐 Canlı veri")}\n\n" +
                       $"🎯 Kalan hak: {limitResult.Remaining}/10";

            await _bot.SendTextMessageAsync(chatId, text,
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Tekrar", $"quick_{from}_{to}"),
                        InlineKeyboardButton.WithCallbackData("🔃 Ters", $"quick_{to}_{from}")
                    }
                }),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _bot.SendTextMessageAsync(chatId, $"❌ Hata: {ex.Message}", cancellationToken: ct);
        }
    }

    public static ReplyKeyboardMarkup GetMainMenuKeyboard() =>
        new(new[]
        {
            new KeyboardButton[] { "💱 Döviz Çevir", "📊 Dashboard'um" },
            new KeyboardButton[] { "📈 Popüler Kurlar", "❓ Yardım" }
        })
        { ResizeKeyboard = true, IsPersistent = true };

    public static InlineKeyboardMarkup GetCurrencyInlineKeyboard(string type, string? exclude = null)
    {
        var currencies = ExchangeService.SupportedCurrencies
            .Where(c => c != exclude).ToList();

        var rows = currencies.Chunk(3)
            .Select(row => row.Select(c =>
                InlineKeyboardButton.WithCallbackData(c, $"{type}_{c}")).ToArray())
            .ToArray();

        return new InlineKeyboardMarkup(rows);
    }
}