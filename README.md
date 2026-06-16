# 💱 Telegram Döviz Botu

Anlık döviz kuru sorgulama, kişisel dashboard ve üyelik sistemi sunan bir Telegram botu. `.NET 8` ve `Clean Architecture` ile geliştirilmiştir.

---

## ✨ Özellikler

- 💱 **Döviz Çevirici** — Desteklenen para birimleri arasında anlık kur çevirme
- 📦 **Akıllı Önbellek** — Aynı kur 5 dakika içinde tekrar sorgulanırsa hak yenmez
- 📊 **Kişisel Dashboard** — Günlük kullanım, haftalık istatistik, favori pariteler
- 📈 **Popüler Kurlar** — USD/TRY, EUR/TRY, GBP/TRY, USD/EUR anlık takip
- 💳 **Üyelik Sistemi** — Free (10/gün), Pro (50/gün), Admin (sınırsız)
- 🚫 **Kullanıcı Engelleme** — Admin tarafından kullanıcı erişimi kısıtlanabilir
- ⌨️ **Çift Menü** — Hem inline butonlar hem de reply klavye desteği

---

## 🤖 Bot Komutları

| Komut | Açıklama |
|-------|----------|
| `/start` | Botu başlat ve karşılama mesajını gör |
| `/menu` | Ana menüyü aç |
| `/convert` | Döviz çevirme işlemi başlat |
| `/dashboard` | Kişisel istatistiklerini gör |
| `/plan` | Üyelik bilgilerini görüntüle |
| `/upgrade` | Pro üyeliğe geçiş bilgisi |
| `/help` | Yardım menüsü |

---

## 🏗️ Mimari

Proje `Clean Architecture` prensiplerine göre 4 katmana ayrılmıştır:

```
TelegramBotProjesi/
├── src/
│   ├── TelegramBot.API/            # Giriş noktası, DI yapılandırması
│   ├── TelegramBot.BotService/     # Bot mantığı (handler'lar, session yönetimi)
│   │   ├── Handlers/
│   │   │   ├── MessageHandler.cs   # Metin mesajlarını işler
│   │   │   └── CallbackHandler.cs  # Inline buton tıklamalarını işler
│   │   └── Sessions/
│   │       └── UserSessionManager.cs
│   ├── TelegramBot.Core/           # Domain modelleri, entity'ler
│   └── TelegramBot.Infrastructure/ # Dış servisler (Exchange API, veritabanı)
│       └── Services/
│           ├── ExchangeService.cs  # Kur verisi + önbellek
│           └── UserService.cs      # Kullanıcı işlemleri
└── TelegramBot.Dashboard/          # Blazor yönetim paneli
```

---

## 🚀 Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Telegram Bot Token ([BotFather](https://t.me/botfather)'dan alınır)
- [ExchangeRate API](https://www.exchangerate-api.com/) anahtarı

### 1. Repoyu klonla

```bash
git clone https://github.com/sualt/TelegramBotProjesi.git
cd TelegramBotProjesi
```

### 2. Ortam değişkenlerini ayarla

`src/TelegramBot.API/` klasöründe `appsettings.json` dosyasını oluştur:

```json
{
  "BotToken": "TELEGRAM_BOT_TOKEN",
  "ExchangeApiKey": "EXCHANGERATE_API_KEY",
  "ConnectionStrings": {
    "DefaultConnection": "VERITABANI_BAGLANTI_DIZESI"
  }
}
```

> ⚠️ `appsettings.json` `.gitignore`'a eklenmiştir. Bu dosyayı asla commit etme.

### 3. Botu çalıştır

```bash
cd src/TelegramBot.API
dotnet run
```

### 4. Dashboard'u çalıştır (opsiyonel)

```bash
cd src/TelegramBot.Dashboard
dotnet run
```

---

## 💳 Üyelik Planları

| Plan | Günlük Limit | Açıklama |
|------|-------------|----------|
| 🆓 Free | 10 sorgu | Varsayılan plan |
| ⭐ Pro | 50 sorgu | Öncelikli destek |
| 🛡️ Admin | Sınırsız | Tam erişim |

---

## 🛠️ Kullanılan Teknolojiler

- **[.NET 8](https://dotnet.microsoft.com/)** — Ana framework
- **[Telegram.Bot 22.0](https://github.com/TelegramBots/Telegram.Bot)** — Telegram API istemcisi
- **[Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** — Yönetim paneli
- **[ExchangeRate-API](https://www.exchangerate-api.com/)** — Döviz kuru verisi

---

## 📄 Lisans

MIT