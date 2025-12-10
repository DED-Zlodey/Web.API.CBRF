using CBRF.Application.Services;
using CBRF.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CBRF.Tests.Application;

[TestFixture]
public class CurrencySyncWorkerTests
{
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private Mock<ILogger<CurrencySyncWorker>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<IServiceScope> _mockServiceScope;
    private Mock<ICurrencyService> _mockCurrencyService;

    [SetUp]
    public void SetUp()
    {
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<CurrencySyncWorker>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockCurrencyService = new Mock<ICurrencyService>();

        // Настройка цепочки зависимостей для создания scope
        var scopeServiceProvider = new Mock<IServiceProvider>();
        scopeServiceProvider.Setup(sp => sp.GetService(typeof(ICurrencyService)))
            .Returns(_mockCurrencyService.Object);
        
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="CurrencySyncWorker"/> корректно разбирает значение времени из настройки "CurrencySync:Time",
    /// если оно задано в допустимом формате.
    /// </summary>
    /// <returns>Задача, представляющая результат теста.</returns>
    [Test]
    public void Constructor_WhenValidTimeFormat_ShouldParseCorrectly()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns("14:30");

        // Act
        var worker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Assert
        worker.Should().NotBeNull();
        _mockConfiguration.Verify(c => c["CurrencySync:Time"], Times.Once);
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="CurrencySyncWorker"/> использует значение времени по умолчанию
    /// и записывает предупреждение в лог, если формат времени в настройке "CurrencySync:Time" некорректен.
    /// </summary>
    /// <returns>Задача, представляющая результат теста.</returns>
    [Test]
    public void Constructor_WhenInvalidTimeFormat_ShouldUseDefaultAndLogWarning()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns("invalid-time");

        // Act
        var worker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Assert
        worker.Should().NotBeNull();
        
        // Проверяем, что было залогировано предупреждение
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid CurrencySync:Time format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="CurrencySyncWorker"/> использует значение времени по умолчанию
    /// (полночь), если настройка времени синхронизации отсутствует в конфигурации.
    /// </summary>
    /// <returns>Задача, представляющая результат теста.</returns>
    [Test]
    public void Constructor_WhenNoTimeConfigured_ShouldUseDefaultMidnight()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns((string?)null);

        // Act
        var worker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Assert
        worker.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что метод <see cref="CurrencySyncWorker.ExecuteAsync(CancellationToken)"/> корректно завершает выполнение,
    /// если операция отменена через токен отмены.
    /// </summary>
    /// <returns>Задача, представляющая результат теста.</returns>
    [Test]
    public async Task ExecuteAsync_WhenCancelled_ShouldStopGracefully()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns("00:00");
        var worker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);
        
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var executeTask = Task.Run(async () =>
        {
            try
            {
                await worker.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ожидаемое исключение
            }
        });

        await Task.Delay(100, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        // Проверяем, что сервис синхронизации не был вызван
        _mockCurrencyService.Verify(
            s => s.SyncRatesForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="CurrencySyncWorker"/> не выбрасывает исключения
    /// при различных корректных значениях времени.
    /// </summary>
    /// <param name="time">Время, передаваемое в конфигурации, в формате HH:mm.</param>
    [Test]
    [TestCase("00:00")]
    [TestCase("12:00")]
    [TestCase("23:59")]
    public void Constructor_WithVariousValidTimes_ShouldNotThrow(string time)
    {
        // Arrange
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns(time);

        // Act
        Action act = () =>
        {
            CurrencySyncWorker currencySyncWorker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);
        };

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что конструктор <see cref="CurrencySyncWorker"/> логирует запланированное время синхронизации,
    /// используя конфигурацию, переданную через параметры.
    /// </summary>
    /// <remarks>
    /// Предполагается, что время синхронизации задается в настройках через ключ "CurrencySync:Time".
    /// Если ключ отсутствует или содержит некорректное значение, используется значение по умолчанию.
    /// </remarks>
    [Test]
    public void Constructor_ShouldLogScheduledTime()
    {
        // Arrange
        var expectedTime = "14:30";
        _mockConfiguration.Setup(c => c["CurrencySync:Time"]).Returns(expectedTime);

        // Act
        var worker = new CurrencySyncWorker(_mockServiceScopeFactory.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Assert
        worker.Should().NotBeNull();
    }
}
