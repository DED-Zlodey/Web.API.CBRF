using System.Net;
using CBRF.Application.Services;
using CBRF.Core.DataModel;
using CBRF.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CBRF.Tests.Application;

[TestFixture]
public class CurrencyServiceTests
{
    /// <summary>
    /// Мок-объект для реализации интерфейса <see cref="ICurrencyRepository"/>.
    /// Используется для модульного тестирования, чтобы предоставить управляемую имитацию работы репозитория.
    /// Позволяет настраивать поведение методов и проверять взаимодействия с репозиторием в процессе тестов.
    /// </summary>
    private Mock<ICurrencyRepository> _mockRepository;

    /// <summary>
    /// Мок объекта ILogger, используемый для имитации логирования в тестах.
    /// </summary>
    private Mock<ILogger<CurrencyService>> _mockLogger;

    /// Локальная переменная, представляющая собой объект Mock для обработки запросов HTTP.
    /// Используется для имитации поведения HttpMessageHandler в модульных тестах, чтобы настроить и проверить
    /// ответы HTTP-запросов без необходимости взаимодействовать с настоящими внешними сервисами.
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;

    /// Экземпляр HttpClient, используемый для выполнения HTTP-запросов.
    /// Предоставляет интерфейс для взаимодействия с внешними API.
    /// В рамках тестов инициализируется с использованием мока HttpMessageHandler,
    /// позволяя контролировать поведение и результаты HTTP-запросов.
    /// Используется сервисом CurrencyService для взаимодействия с API Центробанка России.
    private HttpClient _httpClient;

    /// <summary>
    /// Поле хранит экземпляр сервиса для работы с валютами.
    /// Используется в тестах для проверки функциональности методов сервиса.
    /// </summary>
    private CurrencyService _service;

    /// Инициализирует тестовые данные и создает объекты-заглушки для тестов.
    /// Метод выполняется перед каждым тестом в наборе тестов.
    /// Настраивает:
    /// - Mock-объект для репозитория валют.
    /// - Mock-объект для логгера сервиса валют.
    /// - Mock-объект HttpMessageHandler для тестирования HTTP-запросов.
    /// - HttpClient с базовым адресом для взаимодействия с внешним сервисом.
    /// - Экземпляр CurrencyService, используя созданные mock-объекты.
    /// Данный метод гарантирует изоляцию тестов за счет повторной инициализации данных для каждого тестового случая.
    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<ICurrencyRepository>();
        _mockLogger = new Mock<ILogger<CurrencyService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.cbr.ru/")
        };
        
        _service = new CurrencyService(_mockRepository.Object, _mockLogger.Object, _httpClient);
    }

    /// Освобождает ресурсы, используемые в процессе тестов.
    /// Вызывается после выполнения каждого теста в наборе тестов.
    /// Главная задача метода:
    /// - Освобождение экземпляра HttpClient, созданного во время инициализации теста,
    /// чтобы предотвратить утечки памяти и обеспечить корректную работу в будущем.
    /// Данный метод завершает жизненный цикл объектов, созданных в процессе выполнения теста.
    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    #region GetAllCurrenciesAsync Tests

    /// Проверяет, корректно ли метод возвращает список всех доступных валют.
    /// В рамках теста:
    /// - Имитация работы репозитория для возврата ожидаемого списка валют.
    /// - Выполняется вызов тестируемого метода GetAllCurrenciesAsync из CurrencyService.
    /// - Проверяется:
    /// - Не является ли результат null.
    /// - Соответствует ли количество элементов ожидаемому значению.
    /// - Эквивалентен ли результат ожидаемому списку валют.
    /// - Верифицируется вызов метода репозитория GetAllCurrenciesAsync c указанным временем выполнения.
    [Test]
    public async Task GetAllCurrenciesAsync_ShouldReturnAllCurrencies()
    {
        // Arrange
        var expectedCurrencies = new List<CurrencyRate>
        {
            new() { Id = "R01235", NumCode = 840, CharCode = "USD", Name = "Доллар США", Value = 75.5m, Nominal = 1, VunitRate = 75.5m },
            new() { Id = "R01239", NumCode = 978, CharCode = "EUR", Name = "Евро", Value = 90.0m, Nominal = 1, VunitRate = 90.0m }
        };
        
        _mockRepository.Setup(r => r.GetAllCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrencies);

        // Act
        var result = await _service.GetAllCurrenciesAsync();

        // Assert
        var currencyRates = result.ToList();
        currencyRates.Should().NotBeNull();
        currencyRates.Should().HaveCount(2);
        currencyRates.Should().BeEquivalentTo(expectedCurrencies);
        _mockRepository.Verify(r => r.GetAllCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Проверяет, что метод GetAllCurrenciesAsync возвращает пустую коллекцию, если репозиторий возвращает пустой набор данных.
    /// Тест имитирует поведение репозитория, возвращающего пустой список валют.
    /// Выполняет следующие шаги:
    /// - Настраивает мок-объект репозитория на возврат пустой коллекции.
    /// - Вызывает метод GetAllCurrenciesAsync.
    /// - Проверяет, что возвращаемая коллекция не null и пуста.
    /// Данный тест гарантирует корректную обработку пустых данных из репозитория.
    [Test]
    public async Task GetAllCurrenciesAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyRate>());

        // Act
        var result = await _service.GetAllCurrenciesAsync();

        // Assert
        var currencyRates = result.ToList();
        currencyRates.Should().NotBeNull();
        currencyRates.Should().BeEmpty();
    }

    #endregion

    #region GetCurrencyOrDefaultByNumCodeAsync Tests

    /// Проверяет, что метод GetCurrencyOrDefaultByNumCodeAsync возвращает объект валюты,
    /// если валюта с указанным числовым кодом существует.
    /// Метод выполняет следующие действия:
    /// - Настраивает mock-объект репозитория для возврата ожидаемой валюты по числовому коду.
    /// - Вызывает метод GetCurrencyOrDefaultByNumCodeAsync с тестовым числовым кодом.
    /// - Проверяет, что результат не равен null и эквивалентен ожидаемому объекту.
    /// - Убеждается, что метод репозитория был вызван ровно один раз.
    [Test]
    public async Task GetCurrencyOrDefaultByNumCodeAsync_WhenCurrencyExists_ShouldReturnCurrency()
    {
        // Arrange
        var expectedCurrency = new CurrencyRate 
        { 
            Id = "R01235", 
            NumCode = 840, 
            CharCode = "USD", 
            Name = "Доллар США", 
            Value = 75.5m, 
            Nominal = 1, 
            VunitRate = 75.5m 
        };
        
        _mockRepository.Setup(r => r.GetCurrencyOrDefaultByNumCodeAsync(840, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var result = await _service.GetCurrencyOrDefaultByNumCodeAsync(840);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCurrency);
        _mockRepository.Verify(r => r.GetCurrencyOrDefaultByNumCodeAsync(840, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Проверяет поведение метода получения валюты по цифровому коду, когда валюта не найдена.
    /// Метод имитирует ситуацию, в которой репозиторий возвращает null для указанного цифрового кода валюты.
    /// Включает следующие этапы:
    /// - Настройка mock-объекта репозитория для возврата null при вызове метода получения валюты по переданному цифровому коду.
    /// - Выполнение тестируемого метода.
    /// - Проверка результата на null, что соответствует ожидаемому поведению для случая, когда валюта не найдена.
    /// Тест гарантирует, что метод корректно обрабатывает ситуацию отсутствия валюты в репозитории.
    /// <return>Возвращает null, если валюта с указанным цифровым кодом не существует в репозитории.</return>
    [Test]
    public async Task GetCurrencyOrDefaultByNumCodeAsync_WhenCurrencyNotExists_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetCurrencyOrDefaultByNumCodeAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyRate?)null);

        // Act
        var result = await _service.GetCurrencyOrDefaultByNumCodeAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCurrencyOrDefaultByCharCodeAsync Tests

    /// Проверяет, возвращает ли метод GetCurrencyOrDefaultByCharCodeAsync объект CurrencyRate
    /// при передаче валидного символьного кода валюты.
    /// Тест выполняет следующие шаги:
    /// - Настраивает mock-объект репозитория для возврата заранее определенного объекта CurrencyRate
    /// при вызове метода GetCurrencyOrDefaultByCharCodeAsync с символьным кодом "USD".
    /// - Вызывает данный метод CurrencyService с указанным кодом валюты.
    /// - Проверяет, что результат не равен null.
    /// - Сравнивает возвращенный объект с ожидаемым значением, используя эквивалентность объектов.
    [Test]
    public async Task GetCurrencyOrDefaultByCharCodeAsync_WhenValidCharCode_ShouldReturnCurrency()
    {
        // Arrange
        var expectedCurrency = new CurrencyRate 
        { 
            Id = "R01235", 
            NumCode = 840, 
            CharCode = "USD", 
            Name = "Доллар США", 
            Value = 75.5m, 
            Nominal = 1, 
            VunitRate = 75.5m 
        };
        
        _mockRepository.Setup(r => r.GetCurrencyOrDefaultByCharCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var result = await _service.GetCurrencyOrDefaultByCharCodeAsync("USD");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCurrency);
    }

    /// Проверяет корректность работы метода GetCurrencyOrDefaultByCharCodeAsync,
    /// когда переданный код валюты содержится в нижнем регистре.
    /// Метод должен преобразовать код валюты к верхнему регистру перед выполнением поиска.
    /// Проверка включает:
    /// - Настройку mock-объекта репозитория для возврата ожидаемого объекта CurrencyRate
    /// при запросе с кодом валюты в верхнем регистре.
    /// - Убедиться, что возвращаемое значение не является null.
    /// - Подтверждение, что метод репозитория был вызван с преобразованным кодом валюты.
    [Test]
    public async Task GetCurrencyOrDefaultByCharCodeAsync_WhenLowerCase_ShouldConvertToUpperCase()
    {
        // Arrange
        var expectedCurrency = new CurrencyRate 
        { 
            Id = "R01235", 
            NumCode = 840, 
            CharCode = "USD", 
            Name = "Доллар США", 
            Value = 75.5m, 
            Nominal = 1, 
            VunitRate = 75.5m 
        };
        
        _mockRepository.Setup(r => r.GetCurrencyOrDefaultByCharCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var result = await _service.GetCurrencyOrDefaultByCharCodeAsync("usd");

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.GetCurrencyOrDefaultByCharCodeAsync("USD", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Проверяет поведение метода GetCurrencyOrDefaultByCharCodeAsync в случае,
    /// если переданный код валюты имеет недопустимую длину.
    /// Метод проверяет следующие сценарии:
    /// - Пустое значение.
    /// - Строка, состоящая только из пробелов.
    /// - Код валюты с длиной менее 3 символов.
    /// - Код валюты с длиной более 3 символов.
    /// В случае недопустимой длины код валюты не передается в репозиторий, и метод возвращает null.
    /// <param name="charCode">Код валюты, который нужно проверить.</param>
    [Test]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("AB")]
    [TestCase("ABCD")]
    public async Task GetCurrencyOrDefaultByCharCodeAsync_WhenInvalidLength_ShouldReturnNull(string charCode)
    {
        // Act
        var result = await _service.GetCurrencyOrDefaultByCharCodeAsync(charCode);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetCurrencyOrDefaultByCharCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// Проверяет, что метод GetCurrencyOrDefaultByCharCodeAsync возвращает null,
    /// если валюта с заданным буквенным кодом отсутствует в репозитории.
    /// Настраивает:
    /// - Mock-объект репозитория для возврата null при запросе валюты с кодом "XXX".
    /// Выполняет:
    /// - Вызов метода сервиса с буквенным кодом "XXX".
    /// Проверяет:
    /// - Что результат работы метода равен null.
    [Test]
    public async Task GetCurrencyOrDefaultByCharCodeAsync_WhenCurrencyNotExists_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetCurrencyOrDefaultByCharCodeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyRate?)null);

        // Act
        var result = await _service.GetCurrencyOrDefaultByCharCodeAsync("XXX");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SyncRatesForDateAsync Tests

    /// Проверяет, что метод SyncRatesForDateAsync корректно парсит и сохраняет курсы валют в случае успешного получения данных.
    /// Тест выполняет следующие проверки:
    /// - Подготавливает фиктивный XML-ответ от внешнего сервиса с курсами валют на определенную дату.
    /// - Настраивает mock-объект HttpMessageHandler для возврата подготовленного XML-ответа.
    /// - Настраивает mock-объект репозитория для отслеживания сохраненных курсов.
    /// - Вызывает метод SyncRatesForDateAsync и проверяет:
    /// - Курсы валют успешно сохранены в репозиторий.
    /// - Количество сохраненных курсов соответствует данным в XML.
    /// - Сохраненные данные правильно отображают информацию о валюте, номере, стоимости и дате.
    [Test]
    public async Task SyncRatesForDateAsync_WhenSuccessful_ShouldParseAndSaveRates()
    {
        // Arrange
        var testDate = new DateTime(2024, 12, 10);
        var xmlResponse = @"<?xml version=""1.0"" encoding=""windows-1251""?>
<ValCurs Date=""10.12.2024"" name=""Foreign Currency Market"">
    <Valute ID=""R01235"">
        <NumCode>840</NumCode>
        <CharCode>USD</CharCode>
        <Nominal>1</Nominal>
        <Name>Доллар США</Name>
        <Value>75,5000</Value>
        <VunitRate>75.5000</VunitRate>
    </Valute>
    <Valute ID=""R01239"">
        <NumCode>978</NumCode>
        <CharCode>EUR</CharCode>
        <Nominal>1</Nominal>
        <Name>Евро</Name>
        <Value>90,0000</Value>
        <VunitRate>90.0000</VunitRate>
    </Valute>
</ValCurs>";

        var encoding = System.Text.Encoding.GetEncoding("windows-1251");
        var responseBytes = encoding.GetBytes(xmlResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(responseBytes)
            });

        List<CurrencyRate>? savedRates = null;
        _mockRepository.Setup(r => r.SaveRatesAsync(It.IsAny<IEnumerable<CurrencyRate>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CurrencyRate>, CancellationToken>((rates, ct) => savedRates = rates.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _service.SyncRatesForDateAsync(testDate, CancellationToken.None);

        // Assert
        savedRates.Should().NotBeNull();
        savedRates.Should().HaveCount(2);
        
        var usd = savedRates!.FirstOrDefault(r => r.CharCode == "USD");
        usd.Should().NotBeNull();
        usd!.NumCode.Should().Be(840);
        usd.Value.Should().Be(75.5m);
        usd.Date.Should().Be(new DateOnly(2024, 12, 10));

        if (savedRates != null)
        {
            var eur = savedRates.FirstOrDefault(r => r.CharCode == "EUR");
            eur.Should().NotBeNull();
            eur!.NumCode.Should().Be(978);
            eur.Value.Should().Be(90.0m);
        }

        _mockRepository.Verify(r => r.SaveRatesAsync(It.IsAny<IEnumerable<CurrencyRate>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Проверяет работу метода SyncRatesForDateAsync в случае, если HTTP-запрос завершается неудачей.
    /// Метод должен выбросить исключение HttpRequestException, если сервер возвращает ошибочный статус ответа (например, InternalServerError).
    /// Тест проверяет, что:
    /// - При сбое HTTP-запроса не вызывается метод сохранения курсов валют SaveRatesAsync в репозитории.
    /// Данный тест применяется для проверки обработки ошибок сетевого взаимодействия
    /// и гарантии, что при возникновении исключений данные остаются неизменными.
    [Test]
    public async Task SyncRatesForDateAsync_WhenHttpRequestFails_ShouldThrowException()
    {
        // Arrange
        var testDate = new DateTime(2024, 12, 10);
        
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await _service.Invoking(s => s.SyncRatesForDateAsync(testDate, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();

        _mockRepository.Verify(r => r.SaveRatesAsync(It.IsAny<IEnumerable<CurrencyRate>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// Проверяет обработку ситуации, когда метод SyncRatesForDateAsync получает некорректный XML.
    /// Метод должен выбросить исключение при попытке синхронизации данных с ошибочным форматом XML.
    /// Выполняет следующие действия:
    /// - Имитирует HTTP-ответ с кодом состояния 200 и содержимым, не являющимся корректным XML.
    /// - Убеждается, что метод SyncRatesForDateAsync выбрасывает исключение.
    /// - Проверяет, что метод SaveRatesAsync в репозитории не вызывается.
    [Test]
    public async Task SyncRatesForDateAsync_WhenInvalidXml_ShouldThrowException()
    {
        // Arrange
        var testDate = new DateTime(2024, 12, 10);
        var invalidXml = "This is not XML";
        var encoding = System.Text.Encoding.GetEncoding("windows-1251");
        var responseBytes = encoding.GetBytes(invalidXml);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(responseBytes)
            });

        // Act & Assert
        await _service.Invoking(s => s.SyncRatesForDateAsync(testDate, CancellationToken.None))
            .Should().ThrowAsync<Exception>();

        _mockRepository.Verify(r => r.SaveRatesAsync(It.IsAny<IEnumerable<CurrencyRate>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
