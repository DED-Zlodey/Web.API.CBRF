using CBRF.Core.DataModel;

namespace CBRF.Core.Interfaces;

public interface ICurrencyRepository
{
    /// <summary>
    /// Асинхронно возвращает коллекцию всех доступных курсов валют на максимальную дату
    /// в базе данных без отслеживания изменений.
    /// </summary>
    /// <param name="cts">Токен отмены операции.</param>
    /// <returns>Коллекция объектов типа <see cref="CurrencyRate"/>, содержащая актуальные курсы валют.
    /// В случае ошибки возвращает пустую коллекцию.</returns>
    Task<CurrencyRate[]> GetAllCurrenciesAsync(CancellationToken cts = default);

    /// <summary>
    /// Асинхронно сохраняет предоставленные курсы валют в базу данных.
    /// </summary>
    /// <param name="rates">Коллекция объектов типа <c>CurrencyRate</c>, содержащая данные о курсах валют, которые необходимо сохранить.</param>
    /// <param name="cts">Токен для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию сохранения курсов валют.</returns>
    Task SaveRatesAsync(IEnumerable<CurrencyRate> rates, CancellationToken cts);

    /// <summary>
    /// Асинхронно получает данные о валюте по ее цифровому коду, либо возвращает значение по умолчанию, если данные не найдены.
    /// </summary>
    /// <param name="numCode">Цифровой код валюты, для которой требуется получить данные.</param>
    /// <param name="cts">Токен для отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию, возвращающую объект <c>CurrencyRate</c> или значение по умолчанию, если данные отсутствуют.</returns>
    Task<CurrencyRate?> GetCurrencyOrDefaultByNumCodeAsync(int numCode, CancellationToken cts = default);

    /// <summary>
    /// Асинхронно возвращает объект <see cref="CurrencyRate"/> по заданному символьному коду валюты.
    /// Если валюта с указанным кодом не найдена, возвращает значение по умолчанию (null).
    /// </summary>
    /// <param name="charCode">Символьный код валюты, которую необходимо найти.</param>
    /// <param name="cts">Токен отмены операции.</param>
    /// <returns>Объект типа <see cref="CurrencyRate"/>, содержащий информацию о курсе валюты,
    /// либо значение null, если валюта с заданным кодом не найдена.</returns>
    Task<CurrencyRate?> GetCurrencyOrDefaultByCharCodeAsync(string charCode,
        CancellationToken cts = default);
}