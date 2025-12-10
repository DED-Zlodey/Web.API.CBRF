using CBRF.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CBRF.Application.Services;

public class CurrencySyncWorker : BackgroundService
{
    /// <summary>
    /// Фабрика для создания scope сервисов.
    /// Используется для получения scoped зависимостей в фоновом сервисе.
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Логгер для записи информационных, диагностических и ошибок логов, связанных с работой сервиса синхронизации курсов валют.
    /// Используется для отслеживания выполнения фонового процесса и диагностики проблем.
    /// </summary>
    private readonly ILogger<CurrencySyncWorker> _logger;

    /// <summary>
    /// Переменная, представляющая интервал синхронизации курсов валют в формате TimeSpan.
    /// </summary>
    /// <remarks>
    /// Значение считывается из конфигурации приложения по ключу "CurrencySync:IntervalHours".
    /// Если значение недоступно или имеет неверный формат, используется значение по умолчанию (2 часа).
    /// Используется для определения периодичности автоматической синхронизации курсов валют.
    /// </remarks>
    private readonly TimeSpan _syncTime;


    /// <summary>
    /// Класс, представляющий фоновую службу для синхронизации курсов валют.
    /// </summary>
    public CurrencySyncWorker(IServiceScopeFactory serviceScopeFactory, ILogger<CurrencySyncWorker> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        var hoursStr = configuration["CurrencySync:IntervalHours"];
        if (int.TryParse(hoursStr, out var hours) && hours > 0)
        {
            _syncTime = TimeSpan.FromHours(hours);
        }
        else
        {
            _syncTime = TimeSpan.FromHours(2);
            _logger.LogWarning("{method} Invalid or missing CurrencySync:IntervalHours in config. Using default: 2 hours.", "Constructor");
        }
    }

    /// <summary>
    /// Асинхронный метод, выполняющий основную логику фонового сервиса для синхронизации курсов валют.
    /// </summary>
    /// <param name="stoppingToken">Токен для отслеживания отмены операции.</param>
    /// <returns>Объект Task, представляющий асинхронную операцию.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{method} Currency Sync Worker started. Sync interval: {period}",
            nameof(ExecuteAsync), _syncTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("{method} Starting scheduled currency sync...", nameof(ExecuteAsync));

                // Создаем скоуп, получаем сервис и выполняем работу
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
                    await service.SyncRatesForDateAsync(DateTime.UtcNow, stoppingToken);
                }

                _logger.LogInformation("{method} Sync completed successfully. Next sync in {period}.", 
                    nameof(ExecuteAsync), _syncTime);
            }
            catch (TaskCanceledException)
            {
                // Нормальное завершение при остановке приложения
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{method} Error occurred during currency sync. Retrying in {period}.", 
                    nameof(ExecuteAsync), _syncTime);
            }

            // Ждем указанный интервал перед следующим запуском
            try 
            {
                await Task.Delay(_syncTime, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("{method} Currency Sync Worker is stopping during delay.", nameof(ExecuteAsync));
                break;
            }
        }
    }
}