# CBRF.API - Сервис курсов валют ЦБ РФ

API-сервис для получения и синхронизации курсов валют Центрального Банка Российской Федерации. Проект построен на .NET 10 с поддержкой Native AOT компиляции для максимальной производительности.

## 🚀 Особенности

- **Native AOT компиляция** - быстрый старт и минимальное потребление памяти
- **Автоматическая синхронизация** - ежедневное обновление курсов валют по расписанию
- **PostgreSQL** - надежное хранение данных
- **Structured Logging** - логирование через Serilog с отправкой в Seq
- **OpenAPI** - автоматическая документация API в режиме разработки
- **Minimal API** - современный подход к построению веб-сервисов

## 📋 Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 12 или выше
- (Опционально) [Seq](https://datalust.co/seq) для просмотра логов

## 🔧 Установка и настройка

### 1. Клонирование репозитория

```bash
git clone <repository-url>
cd WebCBRFServiceAOT
```

### 2. Настройка базы данных

Создайте базу данных PostgreSQL:

```sql
CREATE DATABASE cbrf_currencies;
CREATE USER cbrf_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE cbrf_currencies TO cbrf_user;
```

### 3. Применение миграций

Перейдите в директорию `Migrator` и выполните миграции:

```bash
cd Migrator
dotnet run
```

### 4. Настройка конфигурации

Отредактируйте файл `appsettings.json` или создайте `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "PostgreConnection": "Server=localhost;Port=5432;Database=cbrf_currencies;User ID=cbrf_user;Password=your_password"
  },
  "SeqUrl": "http://localhost:5341",
  "CurrencySync": {
    "Time": "00:00",
    "AdminPassword": "your-secure-password-here"
  },
  "AllowedHosts": "*"
}
```

#### Параметры конфигурации:

- **ConnectionStrings:PostgreConnection** - строка подключения к PostgreSQL
- **SeqUrl** - URL сервера Seq для логирования (по умолчанию: `http://localhost:5341`)
- **CurrencySync:Time** - время ежедневной синхронизации в формате `HH:mm` (по умолчанию: `00:00`)
- **CurrencySync:AdminPassword** - пароль для принудительного запуска синхронизации

## ▶️ Запуск

### Режим разработки

```bash
cd CBRF.API
dotnet run
```

Приложение будет доступно по адресу: `http://localhost:5101`

### Режим production с AOT

```bash
cd CBRF.API
dotnet publish -c Release -r win-x64
cd bin\Release\net10.0\win-x64\native
.\CBRF.API.exe
```

## 📚 API Endpoints

### Получить все валюты

```http
GET /api/currencies
```

**Ответ:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "numCode": 840,
    "charCode": "USD",
    "nominal": 1,
    "name": "Доллар США",
    "value": 75.50,
    "vunitRate": 75.50,
    "date": "2024-12-10T00:00:00Z"
  }
]
```

### Получить валюту по числовому коду

```http
GET /api/currencies/{numCode}
```

**Пример:**
```http
GET /api/currencies/840
```

**Ответ:** объект валюты или `404 Not Found`

### Получить валюту по символьному коду

```http
GET /api/currencies/{charCode}
```

**Пример:**
```http
GET /api/currencies/USD
```

**Ответ:** объект валюты или `404 Not Found`

### Принудительная синхронизация (требует авторизации)

```http
POST /api/currencies/sync
X-Admin-Password: your-secure-password-here
```

**Ответ:**
```json
{
  "message": "Sync started",
  "timestamp": "2024-12-10T12:00:00Z"
}
```

**Коды ответов:**
- `200 OK` - синхронизация запущена
- `401 Unauthorized` - неверный пароль
- `500 Internal Server Error` - пароль не настроен

## 🔄 Автоматическая синхронизация

Сервис автоматически синхронизирует курсы валют с сайта ЦБ РФ ежедневно в указанное время (по умолчанию в 00:00). Время можно настроить в конфигурации:

```json
{
  "CurrencySync": {
    "Time": "09:30"
  }
}
```

## 🏗️ Архитектура проекта

Проект следует принципам Clean Architecture и разделен на слои:

- **CBRF.API** - веб-API и точка входа
- **CBRF.Application** - бизнес-логика и сервисы
- **CBRF.Core** - доменные модели и интерфейсы
- **CBRF.DataLayer** - работа с данными
- **CBRF.Repositories** - реализация репозиториев
- **CBRF.Tests** - unit и integration тесты
- **Migrator** - запуск миграций

## 🧪 Тестирование

Запуск всех тестов:

```bash
dotnet test
```

Запуск с покрытием кода:

```bash
dotnet test /p:CollectCoverage=true
```

## 📊 Логирование

Приложение использует Serilog для структурированного логирования. Логи отправляются в:

- Консоль (в режиме разработки)
- Seq сервер (если настроен)

Для просмотра логов в Seq:
1. Установите и запустите Seq: https://datalust.co/seq
2. Откройте http://localhost:5341 в браузере

## 🔒 Безопасность

- Используйте надежный пароль в `CurrencySync:AdminPassword`
- Не коммитьте `appsettings.json` с реальными паролями в репозиторий
- Используйте переменные окружения или секреты для production окружения
- Настройте HTTPS для production развертывания

## 🐳 Docker (опционально)

Создайте `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CBRF.API.dll"]
```

## 📝 Примеры использования

### cURL

```bash
# Получить все валюты
curl http://localhost:5101/api/currencies

# Получить USD
curl http://localhost:5101/api/currencies/USD

# Принудительная синхронизация
curl -X POST http://localhost:5101/api/currencies/sync \
  -H "X-Admin-Password: your-secure-password-here"
```

### PowerShell

```powershell
# Получить все валюты
Invoke-RestMethod -Uri "http://localhost:5101/api/currencies" -Method Get

# Получить EUR
Invoke-RestMethod -Uri "http://localhost:5101/api/currencies/EUR" -Method Get

# Принудительная синхронизация
$headers = @{ "X-Admin-Password" = "your-secure-password-here" }
Invoke-RestMethod -Uri "http://localhost:5101/api/currencies/sync" -Method Post -Headers $headers
```

### C#

```csharp
using System.Net.Http;
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5101") };

// Получить все валюты
var currencies = await client.GetFromJsonAsync<List<CurrencyRate>>("/api/currencies");

// Получить USD
var usd = await client.GetFromJsonAsync<CurrencyRate>("/api/currencies/USD");

// Принудительная синхронизация
var request = new HttpRequestMessage(HttpMethod.Post, "/api/currencies/sync");
request.Headers.Add("X-Admin-Password", "your-secure-password-here");
var response = await client.SendAsync(request);
```

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. См. файл `LICENSE` для подробностей.

## 📞 Контакты

При возникновении вопросов или проблем создайте Issue в репозитории проекта.

---

**Примечание:** Данные о курсах валют предоставляются Центральным Банком Российской Федерации и обновляются ежедневно.
