using CBRF.Core.DataModel;
using CBRF.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CBRF.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    /// <summary>
    /// Поле, используемое для управления подключением к источнику данных PostgreSQL.
    /// Представляет собой объект типа NpgsqlDataSource, предоставляющий пул соединений,
    /// выполнения SQL-команд и взаимодействия с базой данных.
    /// </summary>
    private readonly NpgsqlDataSource _dataSource;

    /// <summary>
    /// Логгер, используемый для записи информационных, отладочных и диагностических сообщений,
    /// а также ошибок, возникающих в процессе работы репозитория валютных курсов.
    /// </summary>
    private readonly ILogger<CurrencyRepository> _logger;

    /// <summary>
    /// Репозиторий для работы с данными валютных курсов.
    /// Предоставляет методы для добавления данных о валютных курсах в базу данных.
    /// </summary>
    public CurrencyRepository(NpgsqlDataSource dataSource, ILogger<CurrencyRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<IEnumerable<CurrencyRate>> GetAllCurrenciesAsync(CancellationToken cts = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cts);

        try
        {
            await using var command = connection.CreateCommand();
            
            command.CommandText = """
                                      SELECT "Id", "CharCode", "Date", "Name", "Nominal", "NumCode", "Value", "VunitRate"
                                      FROM currency_rates 
                                      WHERE "Date" = (SELECT MAX("Date") FROM currency_rates)
                                  """;

            await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cts);

            var result = new List<CurrencyRate>();

            while (await reader.ReadAsync(cts))
            {
                result.Add(new CurrencyRate
                {
                    Id = reader.GetString(0),
                    CharCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Date = reader.GetFieldValue<DateOnly>(2),
                    Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Nominal = reader.GetInt32(4),
                    NumCode = reader.GetInt32(5),
                    Value = reader.GetDecimal(6),
                    VunitRate = reader.GetDecimal(7)
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{method} Error executing raw SQL via ADO.NET", nameof(GetAllCurrenciesAsync));
            throw;
        }
    }
    
    public async Task SaveRatesAsync(IEnumerable<CurrencyRate> rates, CancellationToken cts)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cts);

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cts);

        await using var transaction = await connection.BeginTransactionAsync(cts);

        try
        {
            foreach (var rate in rates)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText = """
                                          INSERT INTO currency_rates (
                                              "Id", "NumCode", "CharCode", "Nominal", "Name", "Value", "VunitRate", "Date"
                                          ) VALUES (
                                              @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7
                                          )
                                          ON CONFLICT ("Id") DO UPDATE 
                                          SET "Value" = EXCLUDED."Value", 
                                              "VunitRate" = EXCLUDED."VunitRate",
                                              "Date" = EXCLUDED."Date";
                                      """;


                // Хелпер для добавления параметров
                void AddParam(string name, object? value)
                {
                    var p = command.CreateParameter();
                    p.ParameterName = name;
                    p.Value = value ?? DBNull.Value;
                    command.Parameters.Add(p);
                }

                AddParam("p0", rate.Id);
                AddParam("p1", rate.NumCode);
                AddParam("p2", rate.CharCode);
                AddParam("p3", rate.Nominal);
                AddParam("p4", rate.Name);
                AddParam("p5", rate.Value);
                AddParam("p6", rate.VunitRate);
                AddParam("p7", rate.Date);

                await command.ExecuteNonQueryAsync(cts);
            }

            await transaction.CommitAsync(cts);
        }
        catch
        {
            _logger.LogError("{method} Error saving rates to database", nameof(SaveRatesAsync));
            await transaction.RollbackAsync(cts);
            throw;
        }
        finally
        {
            if (connection.State == System.Data.ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task<CurrencyRate?> GetCurrencyOrDefaultByNumCodeAsync(int numCode, CancellationToken cts = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cts);

        await using var command = connection.CreateCommand();
        
        command.CommandText = """
                                  SELECT "Id", "CharCode", "Date", "Name", "Nominal", "NumCode", "Value", "VunitRate"
                                  FROM currency_rates 
                                  WHERE "NumCode" = @p0 
                                  ORDER BY "Date" DESC 
                                  LIMIT 1
                              """;

        var p0 = command.CreateParameter();
        p0.ParameterName = "p0";
        p0.Value = numCode;
        command.Parameters.Add(p0);

        await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | System.Data.CommandBehavior.SingleRow, cts);

        if (await reader.ReadAsync(cts))
        {
            return new CurrencyRate
            {
                Id = reader.GetString(0),
                CharCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                Date = reader.GetFieldValue<DateOnly>(2),
                Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                Nominal = reader.GetInt32(4),
                NumCode = reader.GetInt32(5),
                Value = reader.GetDecimal(6),
                VunitRate = reader.GetDecimal(7)
            };
        }

        return null;
    }

    public async Task<CurrencyRate?> GetCurrencyOrDefaultByCharCodeAsync(string charCode,
        CancellationToken cts = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cts);

        await using var command = connection.CreateCommand();
        
        command.CommandText = """
                                  SELECT "Id", "CharCode", "Date", "Name", "Nominal", "NumCode", "Value", "VunitRate"
                                  FROM currency_rates 
                                  WHERE "CharCode" = @p0 
                                  ORDER BY "Date" DESC 
                                  LIMIT 1
                              """;

        var p0 = command.CreateParameter();
        p0.ParameterName = "p0";
        p0.Value = charCode;
        command.Parameters.Add(p0);

        await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | System.Data.CommandBehavior.SingleRow, cts);

        if (await reader.ReadAsync(cts))
        {
            return new CurrencyRate
            {
                Id = reader.GetString(0),
                CharCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                Date = reader.GetFieldValue<DateOnly>(2),
                Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                Nominal = reader.GetInt32(4),
                NumCode = reader.GetInt32(5),
                Value = reader.GetDecimal(6),
                VunitRate = reader.GetDecimal(7)
            };
        }

        return null;
    }
}