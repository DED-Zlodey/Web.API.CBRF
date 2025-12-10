using System.Globalization;
using System.Text;
using System.Xml.Linq;
using CBRF.Core.DataModel;
using CBRF.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CBRF.Application.Services;

public class CurrencyService : ICurrencyService
{
    /// <summary>
    /// Репозиторий для работы с данными о валютах.
    /// Предоставляет методы для получения, добавления и управления данными о курсах валют.
    /// Используется для взаимодействия с базой данных.
    /// </summary>
    private readonly ICurrencyRepository _currencyRepository;

    /// <summary>
    /// Логгер для записи диагностических сообщений и ошибок, возникающих в процессе работы сервиса валют.
    /// Используется для отслеживания информации о состоянии выполнения операций и отладки.
    /// </summary>
    private readonly ILogger<CurrencyService> _logger;

    /// <summary>
    /// Экземпляр HttpClient, используемый для выполнения HTTP-запросов.
    /// Предоставляет методы для взаимодействия с внешними API, такими как получение данных о курсах валют с удаленного сервера.
    /// Управляет созданием и отправкой запросов, а также обработкой ответов.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Сервис для работы с данными о валютах, включая их получение, добавление
    /// и массовое добавление курсов валют. Реализует основные функции взаимодействия
    /// с репозиторием валют.
    /// </summary>
    public CurrencyService(ICurrencyRepository currencyRepository, ILogger<CurrencyService> logger,
        HttpClient httpClient)
    {
        _currencyRepository = currencyRepository;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<CurrencyRate>> GetAllCurrenciesAsync(CancellationToken cts = default)
    {
        return await _currencyRepository.GetAllCurrenciesAsync(cts);
    }

    public async Task<CurrencyRate?> GetCurrencyOrDefaultByNumCodeAsync(int numCode, CancellationToken cts = default)
    {
        return await _currencyRepository.GetCurrencyOrDefaultByNumCodeAsync(numCode, cts);
    }

    public async Task SyncRatesForDateAsync(DateTime date, CancellationToken ct)
    {
        try
        {
            var dateStr = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            var url = $"https://www.cbr.ru/scripts/XML_daily.asp?date_req={dateStr}";
            
            var bytes = await _httpClient.GetByteArrayAsync(url, ct);

            // Декодируем Windows-1251
            var encoding = Encoding.GetEncoding("windows-1251");
            var xmlString = encoding.GetString(bytes);

            // Парсим XML через XDocument (LINQ to XML)
            var xdoc = XDocument.Parse(xmlString);

            // Получаем корневой элемент
            var root = xdoc.Element("ValCurs");
            if (root == null) throw new Exception("XML does not contain ValCurs root");

            // Дата актуальности из XML
            var dateAttr = root.Attribute("Date")?.Value;
            var rateDate = DateOnly.ParseExact(dateAttr!, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            var rates = new List<CurrencyRate>();

            // Настройка парсинга чисел (в XML запятая как разделитель)
            var ruCulture = CultureInfo.GetCultureInfo("ru-RU");

            foreach (var element in root.Elements("Valute"))
            {
                var rate = new CurrencyRate
                {
                    Id = element.Attribute("ID")?.Value ?? throw new Exception("No ID"),
                    NumCode = int.Parse(element.Element("NumCode")?.Value ?? "0"),
                    CharCode = element.Element("CharCode")?.Value,
                    Nominal = int.Parse(element.Element("Nominal")?.Value ?? "1"),
                    Name = element.Element("Name")?.Value,
                    Value = decimal.Parse(element.Element("Value")?.Value ?? "0", ruCulture),
                    VunitRate = decimal.Parse(element.Element("VunitRate")?.Value.Replace(",", ".") ?? "0",
                        CultureInfo.InvariantCulture),
                    Date = rateDate
                };

                rates.Add(rate);
            }

            _logger.LogInformation("Parsed {Count} rates for {Date}", rates.Count, rateDate);
            
            await _currencyRepository.SaveRatesAsync(rates, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing rates from CBRF");
            throw;
        }
    }

    public async Task<CurrencyRate?> GetCurrencyOrDefaultByCharCodeAsync(string charCode,
        CancellationToken cts = default)
    {
        if (charCode.Length != 3 || string.IsNullOrWhiteSpace(charCode))
            return null;
        charCode = string.Create(3, charCode, static (span, code) =>
        {
            code.AsSpan().ToUpperInvariant(span);
        });
        return await _currencyRepository.GetCurrencyOrDefaultByCharCodeAsync(charCode, cts);
    }
}