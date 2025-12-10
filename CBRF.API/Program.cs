using System.Text.Json.Serialization;
using CBRF.Application.Services;
using CBRF.Core.DataModel;
using CBRF.Core.DTOs;
using CBRF.Core.Interfaces;
using CBRF.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Npgsql;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .CreateLogger();

WebApplication app;

try
{
    var builder = WebApplication.CreateSlimBuilder(args);

    var seqUrl = builder.Configuration["SeqUrl"] ?? "http://localhost:5341";

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CBRF.AOT.Service")
        .Enrich.WithEnvironmentName()
        .WriteTo.Seq(seqUrl)
        .CreateLogger();


    builder.Host.UseSerilog();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        // Обрабатываем стандартные заголовки X-Forwarded-For (IP) и X-Forwarded-Proto (Schema/HTTPS)
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });


    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });

    builder.Services.AddOutputCache(options =>
    {
        // Создаем политику "RatesPolicy"
        options.AddPolicy("RatesPolicy", builder =>
            builder.Expire(TimeSpan.FromMinutes(30))
                .Tag("currencies_tag"));
    });

    var connString = builder.Configuration.GetConnectionString("PostgreConnection");

    builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
        NpgsqlDataSource.Create(connString!));

    builder.Services.AddOpenApi();

    builder.Services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

        client.BaseAddress = new Uri("https://www.cbr.ru/");
    });
    builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
    builder.Services.AddScoped<ICurrencyService, CurrencyService>();
    builder.Services.AddHostedService<CurrencySyncWorker>();

    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    app = builder.Build();

    app.UseForwardedHeaders();

    app.UseOutputCache();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    var api = app.MapGroup("/api/currencies");
    api.MapGet("/",
            async (ICurrencyService service, CancellationToken ct) =>
            {
                return await service.GetAllCurrenciesAsync(ct);
            })
        .CacheOutput("RatesPolicy");

    // Получить по NumCode
    api.MapGet("/{numCode:int}", async (int numCode, ICurrencyService service, CancellationToken ct) =>
        {
            var result = await service.GetCurrencyOrDefaultByNumCodeAsync(numCode, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("RatesPolicy");

    // Получить по CharCode
    api.MapGet("/{charCode}", async (string charCode, ICurrencyService service, CancellationToken ct) =>
        {
            var result = await service.GetCurrencyOrDefaultByCharCodeAsync(charCode, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("RatesPolicy");

    // Принудительный запуск обновления (для админа/тестов)
    api.MapPost("/sync", async (
        ICurrencyService service,
        IConfiguration configuration,
        HttpContext context,
        IOutputCacheStore cacheStore,
        CancellationToken ct) =>
    {
        // Проверяем пароль из заголовка или query параметра
        var providedPassword = context.Request.Headers["X-Admin-Password"].FirstOrDefault();

        var correctPassword = configuration["CurrencySync:AdminPassword"];

        if (string.IsNullOrEmpty(correctPassword))
        {
            return Results.Problem("Admin password not configured", statusCode: 500);
        }

        if (providedPassword != correctPassword)
        {
            return Results.Unauthorized();
        }

        await service.SyncRatesForDateAsync(DateTime.UtcNow, ct);
        await cacheStore.EvictByTagAsync("currencies_tag", ct);
        return Results.Ok(new SyncResponse("Sync started", DateTime.UtcNow));
    });
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

[JsonSerializable(typeof(List<CurrencyRate>))]
[JsonSerializable(typeof(CurrencyRate))]
[JsonSerializable(typeof(IEnumerable<CurrencyRate>))]
[JsonSerializable(typeof(SyncResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

// Делаем Program публичным для integration тестов
public partial class Program
{
}