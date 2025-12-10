# Тесты для CBRF Service

## Структура тестов

### Application Layer Tests
- **CurrencyServiceTests.cs** - Unit тесты для `CurrencyService`
  - Тесты получения всех валют
  - Тесты получения валюты по NumCode
  - Тесты получения валюты по CharCode (с валидацией)
  - Тесты синхронизации курсов (успешные и с ошибками)

- **CurrencySyncWorkerTests.cs** - Unit тесты для `CurrencySyncWorker`
  - Тесты парсинга конфигурации времени синхронизации
  - Тесты обработки ошибок
  - Тесты отмены операций

### API Layer Tests
- **CurrencyApiTests.cs** - Integration тесты для API endpoints
  - GET /api/currencies
  - GET /api/currencies/{numCode}
  - GET /api/currencies/{charCode}
  - POST /api/currencies/sync (с авторизацией)

## Запуск тестов

### Через командную строку
```bash
# Из корневой директории проекта
dotnet test CBRF.Tests/CBRF.Tests.csproj --verbosity normal

# С детальным выводом
dotnet test CBRF.Tests/CBRF.Tests.csproj --verbosity detailed

# Только конкретный тестовый класс
dotnet test CBRF.Tests/CBRF.Tests.csproj --filter "FullyQualifiedName~CurrencyServiceTests"
```

### Через Rider
1. Откройте Test Explorer (View -> Tool Windows -> Unit Tests)
2. Нажмите "Run All Tests" или выберите конкретные тесты
3. Результаты появятся в окне Unit Tests

### Через Visual Studio
1. Откройте Test Explorer (Test -> Test Explorer)
2. Нажмите "Run All" или выберите конкретные тесты

## Используемые библиотеки

- **NUnit 4.3.2** - Фреймворк для тестирования
- **Moq 4.20.72** - Библиотека для создания mock-объектов
- **FluentAssertions 7.0.0** - Библиотека для читаемых assertions
- **Microsoft.AspNetCore.Mvc.Testing 10.0.0** - Для integration тестирования API

## Покрытие

Тесты покрывают:
- ✅ Все публичные методы CurrencyService
- ✅ Все публичные методы CurrencySyncWorker
- ✅ Все API endpoints
- ✅ Обработку ошибок
- ✅ Валидацию входных данных
- ✅ Авторизацию для защищенных endpoints

## Примечания

- API тесты используют `WebApplicationFactory` для создания тестового веб-сервера
- Все зависимости заменены на mock-объекты для изоляции тестов
- Тесты не требуют реальной базы данных или внешних сервисов
