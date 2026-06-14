using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.BotService.Handlers;

namespace TelegramBot.BotService;

public class BotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly MessageHandler _messageHandler;
    private readonly CallbackHandler _callbackHandler;
    private readonly ILogger<BotHostedService> _logger;

    public BotHostedService(
        ITelegramBotClient botClient,
        MessageHandler messageHandler,
        CallbackHandler callbackHandler,
        ILogger<BotHostedService> logger)
    {
        _botClient = botClient;
        _messageHandler = messageHandler;
        _callbackHandler = callbackHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Telegram botu başlatılıyor...");

        await _botClient.SetMyCommands(new[]
{
    new BotCommand { Command = "start",     Description = "Botu başlat" },
    new BotCommand { Command = "menu",      Description = "Ana menü" },
    new BotCommand { Command = "convert",   Description = "Döviz çevir" },
    new BotCommand { Command = "dashboard", Description = "İstatistiklerim" },
    new BotCommand { Command = "help",      Description = "Yardım" }
}, cancellationToken: stoppingToken);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            },
            cts.Token
        );

        _logger.LogInformation("✅ Bot aktif");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message != null)
                await _messageHandler.HandleAsync(update.Message, ct);

            if (update.CallbackQuery != null)
                await _callbackHandler.HandleAsync(update.CallbackQuery, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update işlenirken hata");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Bot polling hatası");
        return Task.CompletedTask;
    }
}