using CBRF.Core.DataModel;
using CBRF.DataLayer.ModelConfigurations;
using Microsoft.EntityFrameworkCore;

namespace CBRF.DataLayer;

public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Контекст базы данных для работы с данными приложения.
    /// Используется для настройки и взаимодействия с базой данных.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    /// <summary>
    /// Набор данных, представляющий курсы валют в системе.
    /// Используется для доступа, управления и сохранения информации о курсах валют.
    /// Содержит такие данные, как код валюты, номинал, название, значение и дату актуальности.
    /// </summary>
    public virtual DbSet<CurrencyRate> CurrencyRates { get; set; }

    /// <summary>
    /// Настраивает модель базы данных и определяет конфигурацию сущностей.
    /// Используется для задания правил и ограничений на уровне базы данных.
    /// </summary>
    /// <param name="builder">
    /// Построитель модели, предоставляющий методы для конфигурации объектов и их взаимосвязей.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new CurrencyRateConfig());
    }
}