using TelegramBot.Core.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.BotService.Sessions;
using TelegramBot.Infrastructure.Services;
using TelegramUser = Telegram.Bot.Types.User;

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
        Console.WriteLine($"MESAJ GELDİ => {message.Text}");
        if (message.From == null || message.Text == null) return;

        var from = message.From;
        var telegramId = from.Id;
        var text = message.Text;
        var chatId = message.Chat.Id;

        await _userService.GetOrCreateUserAsync(telegramId, from.Username, from.FirstName, from.LastName);

        if (await _userService.IsBlockedAsync(telegramId))
        {
            await _bot.SendMessage(chatId,
                "🚫 Hesabın engellenmiştir. @admin ile iletişime geçin.",
                replyMarkup: GetMainMenuKeyboard(),   // ✅ Engellide bile klavye kalsın
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
                // ✅ DÜZELTME 1: Hatalı tutar mesajına da klavye eklendi
                await _bot.SendMessage(chatId,
                    "❌ Geçerli bir sayı gir (örn: 100)",
                    replyMarkup: GetMainMenuKeyboard(),
                    cancellationToken: ct);
            }
            return;
        }

        switch (text)
        {
            case "/menu":
                Console.WriteLine("SWITCH MENU");
                await HandleMenuAsync(chatId, telegramId, ct);
                break;

            case "/start":
                await HandleStartAsync(chatId, from, ct);
                break;

            case "/help":
            case "❓ Yardım":
                await HandleHelpAsync(chatId, telegramId, ct);
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

            case "/upgrade":
                await HandleUpgradeAsync(chatId, telegramId, ct);
                break;

            case "/plan":
            case "💳 Üyeliğim":
                await HandlePlanInfoAsync(chatId, telegramId, ct);
                break;

            // ✅ DÜZELTME 2: Bilinmeyen komut/metin → menüyü göster
            default:
                await _bot.SendMessage(chatId,
                    "❓ Anlamadım. Lütfen aşağıdaki menüyü kullan:",
                    replyMarkup: GetMainMenuKeyboard(),
                    cancellationToken: ct);
                break;
        }
    }

    private async Task HandleStartAsync(long chatId, TelegramUser from, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(from.Id.ToString(), "start");

        var text = $"👋 Merhaba *{from.FirstName}*!\n\n" +
                   "💱 *Döviz Çevirici Bot*'a hoş geldin!\n\n" +
                   "📋 Neler yapabilirsin:\n" +
                   "• Anlık döviz kuru sorgulama\n" +
                   "• Günlük 10 sorgu hakkı\n" +
                   "• 5 dk içinde aynı kur → hak yenmez!\n\n" +
                   "/menu → Ana menüyü aç";

        await _bot.SendMessage(chatId, text,
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
                   "• Aynı kur 5 dk içinde sorgulanırsa hak yenmez 📦\n" +
                   "• Gece yarısı haklar sıfırlanır 🌙";

        await _bot.SendMessage(chatId, text,
            parseMode: ParseMode.Markdown,
            // ✅ Help sonrası da klavye göster
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task HandleMenuAsync(long chatId, long telegramId, CancellationToken ct)
    {
        Console.WriteLine("MENU 1");

        await _userService.LogInteractionAsync(telegramId.ToString(), "menu_open");

        Console.WriteLine("MENU 2");

        var user = await _userService.GetOrCreateUserAsync(telegramId, null, null, null);

        Console.WriteLine("MENU 3");

        var planEmoji = user.Plan switch
        {
            SubscriptionPlan.Pro => "⭐ Pro",
            SubscriptionPlan.Admin => "🛡 Admin",
            _ => "🆓 Free"
        };

        Console.WriteLine("MENU 4");

        await _bot.SendMessage(
            chatId,
            $"📋 *Ana Menü* — {planEmoji}\n\nBir seçenek seç:",
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: ct);

        Console.WriteLine("MENU 5");
    }

    private async Task HandleConvertAsync(long chatId, long telegramId, CancellationToken ct)
    {
        Console.WriteLine("HandleConvertAsync CALISTI");
        await _userService.LogInteractionAsync(telegramId.ToString(), "convert_start");
        Console.WriteLine("Mesaj gonderiliyor...");
        _sessions.SetSession(telegramId, new UserSession { Step = "from" });

        // ✅ Convert başlarken reply keyboard'u gizle (inline keyboard gelecek)
        await _bot.SendMessage(chatId, "💱 Kaynak para birimini seç:",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: ct);

        await _bot.SendMessage(chatId, "Para birimini seç:",
            replyMarkup: GetCurrencyInlineKeyboard("from"),
            cancellationToken: ct);
    }

    private async Task HandleDashboardAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "dashboard_view");

        var id = telegramId.ToString();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var todayUsage    = await _userService.GetTodayUsageAsync(id, today);
        var totalQueries  = await _userService.GetTotalQueriesAsync(id);
        var recentQueries = await _userService.GetRecentQueriesAsync(id, 5);
        var favPairs      = await _userService.GetFavoritePairsAsync(id, 3);
        var weeklyUsage   = await _userService.GetWeeklyUsageAsync(id);

        var remaining = 10 - todayUsage;

        var hakGostergesi = string.Concat(Enumerable.Repeat("🟢", remaining)) +
                            string.Concat(Enumerable.Repeat("⚪", 10 - remaining));

        var haftalikText = "";
        foreach (var (tarih, sayi) in weeklyUsage)
        {
            var gun = DateTime.Parse(tarih).ToString("ddd",
                new System.Globalization.CultureInfo("tr-TR"));
            var bar = string.Concat(Enumerable.Repeat("▓", sayi)) +
                      string.Concat(Enumerable.Repeat("░", 10 - sayi));
            haftalikText += $"\n`{gun}` {bar} {sayi}";
        }

        var favText = favPairs.Any()
            ? string.Join("\n", favPairs.Select((p, i) => $"{i + 1}. {p.Pair} ({p.Count} kez)"))
            : "Henüz yok";

        var sonSorgular = recentQueries.Any()
            ? string.Join("\n", recentQueries.Select(q =>
                $"• {q.FromCurrency}→{q.ToCurrency}: `{q.Rate:F4}` ({q.QueriedAt:HH:mm})"))
            : "Henüz sorgu yapılmadı";

        var text = $"📊 *Kişisel Dashboard*\n" +
                   $"━━━━━━━━━━━━━━━━\n\n" +
                   $"🎯 *Bugünkü Hak*\n" +
                   $"{hakGostergesi}\n" +
                   $"Kullanılan: {todayUsage}/10 — Kalan: {remaining}\n\n" +
                   $"📈 *Genel İstatistik*\n" +
                   $"• Toplam sorgu: {totalQueries}\n\n" +
                   $"⭐ *Favori Paritelerim*\n" +
                   $"{favText}\n\n" +
                   $"📅 *Son 7 Gün*\n" +
                   $"{haftalikText}\n\n" +
                   $"🕐 *Son Sorgular*\n" +
                   $"{sonSorgular}";

        await _bot.SendMessage(chatId, text,
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),   // ✅ Dashboard sonrası klavye geri gelsin
            cancellationToken: ct);
    }

    private async Task HandlePopularRatesAsync(long chatId, long telegramId, CancellationToken ct)
    {
        await _userService.LogInteractionAsync(telegramId.ToString(), "popular_rates");
        await _bot.SendMessage(chatId, "⏳ Kurlar yükleniyor...", cancellationToken: ct);

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
        await _bot.SendMessage(chatId, text,
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),   // ✅ Klavye geri gelsin
            cancellationToken: ct);
    }

    public async Task ProcessConversionAsync(long chatId, long telegramId,
        string from, string to, decimal amount, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "⏳ Kur alınıyor...", cancellationToken: ct);

        try
        {
            var (rate, fromCache) = await _exchangeService.GetRateAsync(from, to);
            var limitResult = await _userService.CheckAndUseLimitAsync(telegramId, fromCache);

            if (!limitResult.Allowed)
            {
                await _bot.SendMessage(chatId,
                    "⛔ Günlük 10 sorgu hakkını tükettiniz!\n🌙 Gece yarısı sıfırlanacak.",
                    replyMarkup: GetMainMenuKeyboard(),   // ✅ Limit dolunca da klavye göster
                    cancellationToken: ct);
                return;
            }

            var result = amount * rate;
            await _userService.SaveQueryAsync(telegramId.ToString(), from, to, amount, rate, result, fromCache);

            var hakBilgisi = fromCache
                ? "📦 Önbellekten geldi — hak kullanılmadı!"
                : $"🎯 Kalan hak: {limitResult.Remaining}/10";

            var text = $"💱 *Döviz Sonucu*\n\n" +
                       $"{amount} *{from}* = *{result:F2} {to}*\n\n" +
                       $"📊 Kur: 1 {from} = {rate:F4} {to}\n" +
                       $"{hakBilgisi}";

            // ✅ DÜZELTME 3: Sonuç mesajında hem inline butonlar hem reply klavye
            // Önce inline butonlu sonuç mesajı
            await _bot.SendMessage(chatId, text,
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

            // ✅ Sonra ayrı mesajla reply klavyeyi geri getir
            await _bot.SendMessage(chatId,
                "Ana menüye dönmek için seçim yap:",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId,
                $"❌ Hata: {ex.Message}",
                replyMarkup: GetMainMenuKeyboard(),   // ✅ Hata sonrası da klavye
                cancellationToken: ct);
        }
    }

    public static ReplyKeyboardMarkup GetMainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[]
            {
                "💱 Döviz Çevir",
                "📊 Dashboard'um"
            },
            new KeyboardButton[]
            {
                "📈 Popüler Kurlar",
                "❓ Yardım"
            },
            new KeyboardButton[]
            {
                "💳 Üyeliğim"
            }
        })
        {
            ResizeKeyboard = true,
        };
    }

    public static InlineKeyboardMarkup GetCurrencyInlineKeyboard(
        string type,
        string? exclude = null)
    {
        var currencies = ExchangeService.SupportedCurrencies
            .Where(c => c != exclude)
            .ToList();

        var rows = currencies
            .Chunk(3)
            .Select(row =>
                row.Select(c =>
                    InlineKeyboardButton.WithCallbackData(
                        c,
                        $"{type}_{c}"
                    )
                ).ToArray()
            )
            .ToArray();

        return new InlineKeyboardMarkup(rows);
    }

    private async Task HandlePlanInfoAsync(long chatId, long telegramId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(telegramId, null, null, null);
        var planName = user.Plan.ToString();
        var expiry = user.PlanExpiresAt.HasValue
            ? user.PlanExpiresAt.Value.ToString("dd.MM.yyyy")
            : "Süresiz";
        var limit = user.Plan == SubscriptionPlan.Pro ? 50 : user.Plan == SubscriptionPlan.Admin ? 9999 : 10;

        var text = $"💳 *Üyelik Bilgisi*\n\n" +
                   $"Plan: *{planName}*\n" +
                   $"Günlük limit: *{limit} sorgu*\n" +
                   $"Geçerlilik: *{expiry}*\n\n" +
                   (user.Plan == SubscriptionPlan.Free
                       ? "⬆️ Pro'ya geçmek için /upgrade"
                       : "✅ Aktif üyeliğin var!");

        await _bot.SendMessage(chatId, text,
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),   // ✅ Plan sonrası klavye
            cancellationToken: ct);
    }

    private async Task HandleUpgradeAsync(long chatId, long telegramId, CancellationToken ct)
    {
        var text = "⭐ *Pro Üyelik*\n\n" +
                   "• Günlük 50 sorgu hakkı\n" +
                   "• Öncelikli destek\n" +
                   "• Daha fazla para birimi\n\n" +
                   "Üyelik için @admin ile iletişime geçin.";

        await _bot.SendMessage(chatId, text,
            parseMode: ParseMode.Markdown,
            replyMarkup: GetMainMenuKeyboard(),   // ✅ Upgrade sonrası klavye
            cancellationToken: ct);
    }
}