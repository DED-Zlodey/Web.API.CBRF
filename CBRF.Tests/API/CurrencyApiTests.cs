using System.Net;
using System.Net.Http.Json;
using CBRF.Core.DataModel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CBRF.Core.Interfaces;
using Moq;

namespace CBRF.Tests.API;

[TestFixture]
public class CurrencyApiTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private Mock<ICurrencyService> _mockCurrencyService;

    [SetUp]
    public void SetUp()
    {
        _mockCurrencyService = new Mock<ICurrencyService>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Добавляем конфигурацию для тестов
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["CurrencySync:AdminPassword"] = "test-password"
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Удаляем ВСЕ регистрации ICurrencyService (может быть зарегистрирован через AddHttpClient и AddScoped)
                    var descriptors = services.Where(d => d.ServiceType == typeof(ICurrencyService)).ToList();
                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Добавляем mock
                    services.AddScoped<ICurrencyService>(_ => _mockCurrencyService.Object);
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region GET /api/currencies

    [Test]
    public async Task GetAllCurrencies_ShouldReturnOkWithCurrencies()
    {
        // Arrange
        var expectedCurrencies = new List<CurrencyRate>
        {
            new() { Id = "R01235", NumCode = 840, CharCode = "USD", Name = "Доллар США", Value = 75.5m, Nominal = 1, VunitRate = 75.5m },
            new() { Id = "R01239", NumCode = 978, CharCode = "EUR", Name = "Евро", Value = 90.0m, Nominal = 1, VunitRate = 90.0m }
        };

        _mockCurrencyService.Setup(s => s.GetAllCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrencies);

        // Act
        var response = await _client.GetAsync("/api/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currencies = await response.Content.ReadFromJsonAsync<List<CurrencyRate>>();
        currencies.Should().NotBeNull();
        currencies.Should().HaveCount(2);
        currencies.Should().BeEquivalentTo(expectedCurrencies);
    }

    [Test]
    public async Task GetAllCurrencies_WhenEmpty_ShouldReturnOkWithEmptyArray()
    {
        // Arrange
        _mockCurrencyService.Setup(s => s.GetAllCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyRate>());

        // Act
        var response = await _client.GetAsync("/api/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currencies = await response.Content.ReadFromJsonAsync<List<CurrencyRate>>();
        currencies.Should().NotBeNull();
        currencies.Should().BeEmpty();
    }

    #endregion

    #region GET /api/currencies/{numCode}

    [Test]
    public async Task GetCurrencyByNumCode_WhenExists_ShouldReturnOk()
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

        _mockCurrencyService.Setup(s => s.GetCurrencyOrDefaultByNumCodeAsync(840, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var response = await _client.GetAsync("/api/currencies/840");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currency = await response.Content.ReadFromJsonAsync<CurrencyRate>();
        currency.Should().NotBeNull();
        currency.Should().BeEquivalentTo(expectedCurrency);
    }

    [Test]
    public async Task GetCurrencyByNumCode_WhenNotExists_ShouldReturnNotFound()
    {
        // Arrange
        _mockCurrencyService.Setup(s => s.GetCurrencyOrDefaultByNumCodeAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyRate?)null);

        // Act
        var response = await _client.GetAsync("/api/currencies/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/currencies/{charCode}

    [Test]
    public async Task GetCurrencyByCharCode_WhenExists_ShouldReturnOk()
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

        _mockCurrencyService.Setup(s => s.GetCurrencyOrDefaultByCharCodeAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var response = await _client.GetAsync("/api/currencies/USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currency = await response.Content.ReadFromJsonAsync<CurrencyRate>();
        currency.Should().NotBeNull();
        currency.Should().BeEquivalentTo(expectedCurrency);
    }

    [Test]
    public async Task GetCurrencyByCharCode_WhenNotExists_ShouldReturnNotFound()
    {
        // Arrange
        _mockCurrencyService.Setup(s => s.GetCurrencyOrDefaultByCharCodeAsync("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyRate?)null);

        // Act
        var response = await _client.GetAsync("/api/currencies/XXX");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCurrencyByCharCode_WithLowerCase_ShouldWork()
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

        _mockCurrencyService.Setup(s => s.GetCurrencyOrDefaultByCharCodeAsync("usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCurrency);

        // Act
        var response = await _client.GetAsync("/api/currencies/usd");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/currencies/sync

    [Test]
    public async Task SyncCurrencies_WithValidPassword_ShouldReturnOk()
    {
        // Arrange
        _mockCurrencyService.Setup(s => s.SyncRatesForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/currencies/sync");
        request.Headers.Add("X-Admin-Password", "test-password");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Sync started");
    }

    [Test]
    public async Task SyncCurrencies_WithoutPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/currencies/sync");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncCurrencies_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/currencies/sync");
        request.Headers.Add("X-Admin-Password", "wrong-password");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncCurrencies_WhenServiceThrows_ShouldReturnError()
    {
        // Arrange
        _mockCurrencyService.Setup(s => s.SyncRatesForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Sync failed"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/currencies/sync");
        request.Headers.Add("X-Admin-Password", "test-password");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion
}
