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
    /// Переменная, представляющая время синхронизации курсов валют в формате TimeSpan.
    /// </summary>
    /// <remarks>
    /// Значение считывается из конфигурации приложения по ключу "CurrencySync:Time".
    /// Если значение недоступно или имеет неверный формат, используется значение по умолчанию (00:00).
    /// Используется для вычисления расписания ежедневной синхронизации.
    /// </remarks>
    private readonly TimeSpan _syncTime;

    /// <summary>
    /// Фоновый сервис для ежедневной синхронизации курсов валют.
    /// </summary>
    public CurrencySyncWorker(IServiceScopeFactory serviceScopeFactory, ILogger<CurrencySyncWorker> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        var syncTimeString = configuration["CurrencySync:Time"] ?? "00:00";
        if (TimeSpan.TryParse(syncTimeString, out var parsedTime))
        {
            _syncTime = parsedTime;
        }
        else
        {
            _syncTime = TimeSpan.Zero;
            _logger.LogWarning("{method} Invalid CurrencySync:Time format, using 00:00", "Constructor");
        }
    }

    /// <summary>
    /// Асинхронный метод, выполняющий основную логику фонового сервиса для синхронизации курсов валют.
    /// </summary>
    /// <param name="stoppingToken">Токен для отслеживания отмены операции.</param>
    /// <returns>Объект Task, представляющий асинхронную операцию.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{method} Currency Sync Worker started. Scheduled time: {time}",
            nameof(ExecuteAsync), _syncTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var scheduledTime = now.Date.Add(_syncTime);

            // Если запланированное время уже прошло сегодня, планируем на завтра
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var delay = scheduledTime - now;

            _logger.LogInformation("{method} Next sync scheduled at {scheduledTime} (in {delay})",
                nameof(ExecuteAsync), scheduledTime, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("{method} Starting daily currency sync...", nameof(ExecuteAsync));

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
                    await service.SyncRatesForDateAsync(DateTime.UtcNow, stoppingToken);
                }

                _logger.LogInformation("{method} Sync completed successfully.", nameof(ExecuteAsync));
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("{method} Currency Sync Worker is stopping.", nameof(ExecuteAsync));
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{method} Error occurred during currency sync.", nameof(ExecuteAsync));
            }
        }
    }
}