using Microsoft.Extensions.Options;
using TelegramBot.Infrastructure.MongoDB;
using TelegramBot.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MongoDB ayarları
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// Servisler
builder.Services.AddSingleton<UserService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ExchangeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<TelegramBot.Dashboard.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();