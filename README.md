# 🤖 Telegram Döviz Bot + Dashboard

C# .NET 8, MongoDB ve React ile geliştirilmiş Telegram botu.

## ✨ Özellikler
- 💱 Anlık döviz kuru sorgulama (15+ parite)
- 📊 Kullanıcı kendi dashboard'unu görebilir
- 🔒 Admin panelinden kullanıcı engelleme
- ⚡ 5 dakikalık önbellek sistemi
- 🎯 Günlük 10 sorgu limiti (gece yarısı sıfırlanır)
- 🌐 REST API + Swagger dokümantasyonu
- 🖥️ React Dashboard

## 🏗️ Mimari
Clean Architecture yapısı kullanılmıştır.
TelegramBotProject/

├── src/

│   ├── TelegramBot.API/           → Web API + Swagger

│   ├── TelegramBot.Core/          → Modeller

│   ├── TelegramBot.Infrastructure/→ MongoDB, Servisler

│   └── TelegramBot.BotService/    → Telegram Bot

└── dashboard/                     → React Dashboard

## 🛠️ Teknolojiler
- C# .NET 8
- MongoDB
- Telegram.Bot
- React + Vite

## ⚙️ Kurulum
1. `appsettings.example.json` dosyasını `appsettings.json` olarak kopyala
2. Token ve bağlantı bilgilerini doldur
3. `dotnet run`

## 📝 Notlar
- `appsettings.json` gizlidir, GitHub'a yüklenmez
- Gerçek token bilgileri paylaşılmamaktadır