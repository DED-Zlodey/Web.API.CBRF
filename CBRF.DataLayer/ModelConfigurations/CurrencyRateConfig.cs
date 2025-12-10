using CBRF.Core.DataModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CBRF.DataLayer.ModelConfigurations;

/// <summary>
/// Класс используется для конфигурации сущности CurrencyRate в базе данных.
/// Настраивает отображение свойств сущности и их соответствие столбцам таблицы.
/// Определяет ключи, индексы, ограничения и другие правила работы с данными.
/// </summary>
public class CurrencyRateConfig : IEntityTypeConfiguration<CurrencyRate>
{
    /// Конфигурирует сущность CurrencyRate для использования с Entity Framework.
    /// <param name="builder">
    /// Объект EntityTypeBuilder, предоставляющий функции для настройки свойств и связей сущности.
    /// </param>
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.ToTable("currency_rates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.CharCode)
            .HasMaxLength(3)
            .IsRequired(false);

        builder.Property(x => x.Name)
            .IsRequired(false);

        builder.Property(x => x.Value)
            .IsRequired();

        builder.Property(x => x.Date)
            .HasColumnType("date")
            .IsRequired();
        
        builder.HasIndex(x => x.Date);
        
        builder.HasIndex(x => x.CharCode);
    }
}