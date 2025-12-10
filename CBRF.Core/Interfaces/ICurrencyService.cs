using CBRF.Core.DataModel;

namespace CBRF.Core.Interfaces;

/// <summary>
/// Интерфейс для работы с курсами валют.
/// Предоставляет методы для получения списка валют, поиска по числовому коду,
/// добавления новой валюты и массового добавления курсов валют.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Асинхронно получает список всех доступных курсов валют.
    /// </summary>
    /// <param name="cts">Токен отмены для отмены операции, если требуется.</param>
    /// <returns>Коллекция объектов <see cref="CurrencyRate"/>, представляющих курсы валют.</returns>
    Task<IEnumerable<CurrencyRate>> GetAllCurrenciesAsync(CancellationToken cts = default);

    /// <summary>
    /// Асинхронно возвращает курс валюты по заданному числовому коду.
    /// Если валюты с указанным кодом не найдено, возвращает значение по умолчанию (null).
    /// </summary>
    /// <param name="numCode">Числовой код валюты.</param>
    /// <param name="cts">Токен отмены, позволяющий отменить выполнение операции.</param>
    /// <returns>Объект типа <see cref="CurrencyRate"/>, если валюта найдена, или null, если не найдена.</returns>
    Task<CurrencyRate?> GetCurrencyOrDefaultByNumCodeAsync(int numCode, CancellationToken cts = default);

    /// <summary>
    /// Асинхронно синхронизирует курсы валют для указанной даты.
    /// </summary>
    /// <param name="date">Дата, для которой требуется синхронизация курсов валют.</param>
    /// <param name="ct">Токен отмены, позволяющий отменить выполнение операции.</param>
    /// <returns>Задача, представляющая результат выполнения операции.</returns>
    Task SyncRatesForDateAsync(DateTime date, CancellationToken ct);

    /// <summary>
    /// Асинхронно получает объект <see cref="CurrencyRate"/>, представляющий курс валюты,
    /// по символьному коду валюты, либо возвращает значение по умолчанию, если валюта не найдена.
    /// </summary>
    /// <param name="charCode">Символьный код валюты, который используется для поиска.</param>
    /// <param name="cts">Токен отмены для управления завершением операции, если требуется.</param>
    /// <returns>Объект <see cref="CurrencyRate"/>, представляющий курс валюты, или значение по умолчанию, если валюта не найдена.</returns>
    Task<CurrencyRate?> GetCurrencyOrDefaultByCharCodeAsync(string charCode,
        CancellationToken cts = default);
}