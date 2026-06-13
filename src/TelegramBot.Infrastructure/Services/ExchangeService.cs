// src/TelegramBot.Infrastructure/Services/ExchangeService.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TelegramBot.Infrastructure.Services;

public class ExchangeService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _cacheDuration;

    public static readonly List<string> SupportedCurrencies = new()
    {
        "USD", "EUR", "GBP", "TRY", "JPY",
        "CHF", "CAD", "AUD", "RUB", "SAR",
        "AED", "CNY", "NOK", "SEK", "DKK"
    };

    public ExchangeService(IMemoryCache cache, HttpClient httpClient,
        IConfiguration config, ILogger<ExchangeService> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["Exchange:ApiKey"] 
            ?? throw new ArgumentNullException("Exchange:ApiKey eksik");
        
        // ✅ BaseUrl artık sadece base — key ve endpoint kodda ekleniyor
        _baseUrl = config["Exchange:BaseUrl"] 
            ?? "https://v6.exchangerate-api.com/v6";
        
        var cacheMins = config.GetValue<int>("Exchange:CacheDurationMinutes", 5);
        _cacheDuration = TimeSpan.FromMinutes(cacheMins);
    }

    public async Task<(decimal Rate, bool FromCache)> GetRateAsync(
        string fromCurrency, string toCurrency)
    {
        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency   = toCurrency.ToUpperInvariant();

        if (!SupportedCurrencies.Contains(fromCurrency) || 
            !SupportedCurrencies.Contains(toCurrency))
            throw new ArgumentException($"Desteklenmeyen para birimi: {fromCurrency}/{toCurrency}");

        var cacheKey = $"rate_{fromCurrency}_{toCurrency}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            _logger.LogInformation("Cache hit: {Key}", cacheKey);
            return (cachedRate, true);
        }

        // ✅ Doğru URL: /v6/{apiKey}/pair/{from}/{to}
        var url = $"{_baseUrl}/{_apiKey}/pair/{fromCurrency}/{toCurrency}";
        
        _logger.LogInformation("API isteği: {Url}", url);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Döviz API isteği başarısız: {Url}", url);
            throw new Exception($"Döviz bilgisi alınamadı ({fromCurrency}/{toCurrency})", ex);
        }

        var json = await response.Content.ReadAsStringAsync();
        
        JsonElement data;
        try
        {
            data = JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "API yanıtı parse edilemedi: {Json}", json);
            throw new Exception("Döviz API yanıtı geçersiz format", ex);
        }

        if (data.GetProperty("result").GetString() != "success")
        {
            var errorType = data.TryGetProperty("error-type", out var err) 
                ? err.GetString() 
                : "bilinmeyen hata";
            throw new Exception($"Döviz API hatası: {errorType}");
        }

        var rate = data.GetProperty("conversion_rate").GetDecimal();

        _cache.Set(cacheKey, rate, _cacheDuration);
        _logger.LogInformation("API'den çekildi: {Key} = {Rate}", cacheKey, rate);

        return (rate, false);
    }
}