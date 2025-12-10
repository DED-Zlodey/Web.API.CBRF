using CBRF.DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

string nameFileConfiguration = "appsettings.json";
#if DEBUG
nameFileConfiguration = "appsettings.Development.json";
#endif


var builder = new ConfigurationBuilder()
    .AddJsonFile(nameFileConfiguration, optional: false, reloadOnChange: true);

var config = builder.Build();
var connString = config.GetConnectionString("PostgreConnection"); 

if (string.IsNullOrEmpty(connString))
{
    Console.Error.WriteLine("Connection string is empty!");
    return;
}

// Создаем контекст руками
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql(connString);

await using var context = new ApplicationDbContext(optionsBuilder.Options);

// Накатываем миграции
try 
{
    await context.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully!");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.Message}");
    Environment.Exit(1);
}