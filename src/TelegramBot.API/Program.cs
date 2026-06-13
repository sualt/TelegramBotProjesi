// src/TelegramBot.API/Program.cs
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot.BotService;
using TelegramBot.BotService.Handlers;
using TelegramBot.BotService.Sessions;
using TelegramBot.Infrastructure.MongoDB;
using TelegramBot.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// MongoDB ayarları
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// Servisler
builder.Services.AddSingleton<UserService>();
builder.Services.AddMemoryCache(); // IMemoryCache
builder.Services.AddHttpClient<ExchangeService>();
builder.Services.AddSingleton<UserSessionManager>();

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    new TelegramBotClient(builder.Configuration["Telegram:BotToken"]!));

// Bot Handler'ları
builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddSingleton<CallbackHandler>();

// Bot Background Service (arka planda çalışır)
builder.Services.AddHostedService<BotHostedService>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(); // /swagger → API dökümantasyonu

app.UseCors();
app.MapControllers();
app.Run();