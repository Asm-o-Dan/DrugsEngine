using Application.Catalog;
using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Application.Interfaces.Repositories.DrugRepositories;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Application.UseCases.Commands.DrugCommands;
using Domain.Entities;
using Infrastructure.API.Infrastructure;
using Infrastructure.Configuration;
using Infrastructure.Dal;
using Infrastructure.Dal.Queries;
using Infrastructure.Dal.Repositories.CountryRepositories;
using Infrastructure.Dal.Repositories.DrugItemRepositories;
using Infrastructure.Dal.Repositories.DrugRepositories;
using Infrastructure.Dal.Repositories.DrugStoreRepositories;
using Infrastructure.Dal.Repositories.FavoriteDrugRepositories;
using Infrastructure.Dal.Repositories.UserProfileRepositories;
using Infrastructure.Geocoding;
using Infrastructure.Kafka;
using Infrastructure.Parsing;
using Infrastructure.SemanticSearch;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Services(services)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", context.HostingEnvironment.IsDevelopment()
            ? LogEventLevel.Information
            : LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        // исключения логирует ProblemExceptionHandler — без дублей
        .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    #region Options

    builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));
    builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.Section));
    builder.Services.Configure<ParsingOptions>(builder.Configuration.GetSection(ParsingOptions.Section));
    builder.Services.Configure<SemanticSearchOptions>(builder.Configuration.GetSection(SemanticSearchOptions.Section));
    builder.Services.Configure<GeocodingOptions>(builder.Configuration.GetSection(GeocodingOptions.Section));
    builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.Section));

    #endregion

    #region Database

    var connectionString = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Не задана строка подключения ConnectionStrings:Default (переменная окружения ConnectionStrings__Default).");

    builder.Services.AddDbContext<DrugsBotDbContext>((sp, options) =>
    {
        var db = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        // Без EnableRetryOnFailure: обработчики команд используют явные транзакции.
        options.UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(db.CommandTimeout));
        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging().EnableDetailedErrors();
    });

    #endregion

    #region Repositories & application services

    builder.Services.AddScoped<IDrugWriteRepository, DrugWriteRepository>();
    builder.Services.AddScoped<IDrugReadRepository, DrugReadRepository>();
    builder.Services.AddScoped<IDrugStoreWriteRepository, DrugStoreWriteRepository>();
    builder.Services.AddScoped<IDrugStoreReadRepository, DrugStoreReadRepository>();
    builder.Services.AddScoped<IDrugItemWriteRepository, DrugItemWriteRepository>();
    builder.Services.AddScoped<IDrugItemReadRepository, DrugItemReadRepository>();
    builder.Services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
    builder.Services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();
    builder.Services.AddScoped<IFavoriteDrugWriteRepository, FavoriteDrugWriteRepository>();
    builder.Services.AddScoped<IFavoriteDrugReadRepository, FavoriteDrugReadRepository>();
    builder.Services.AddScoped<ICountryWriteRepository, CountryWriteRepository>();
    builder.Services.AddScoped<ICountryReadRepository, CountryReadRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ICatalogQueries, CatalogQueries>();

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateDrugCommand).Assembly));

    #endregion

    #region Integrations

    builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
    builder.Services.AddHostedService<CatalogPublishWorker>();

    builder.Services.AddHttpClient<ISemanticSearchClient, SemanticSearchClient>((sp, http) =>
    {
        var options = sp.GetRequiredService<IOptions<SemanticSearchOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            http.BaseAddress = new Uri(options.BaseUrl);
        http.Timeout = options.Timeout;
    });

    builder.Services.AddHttpClient<IGeocoder, NominatimGeocoder>((sp, http) =>
    {
        var options = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
        http.BaseAddress = new Uri(options.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(15);
        if (!string.IsNullOrWhiteSpace(options.UserAgent))
            http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    });
    builder.Services.AddScoped<StoreGeocodingService>();

    #endregion

    #region Parsing (фоновая задача)

    builder.Services.AddSingleton<IPharmacyParser, DoctorParser>();
    builder.Services.AddSingleton<IPharmacyParser, StoletnikParser>();
    builder.Services.AddSingleton<ParsingManager>();
    builder.Services.AddHostedService<ParsingWorker>();

    #endregion

    #region HTTP

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ProblemExceptionHandler>();
    builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

    builder.Services.AddControllers()
        .AddOData(options => options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
            .AddRouteComponents("api", GetEdmModel()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    #endregion

    var app = builder.Build();

    if (app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateOnStartup)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
        Log.Information("Применяем миграции базы данных…");
        await db.Database.MigrateAsync();
    }

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.MapHealthChecks("/health");
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Приложение остановлено из-за ошибки при запуске");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static IEdmModel GetEdmModel()
{
    var modelBuilder = new ODataConventionModelBuilder();
    var drugSet = modelBuilder.EntitySet<Drug>("Drug");
    drugSet.EntityType.HasKey(d => d.Id);
    drugSet.EntityType.Ignore(d => d.DrugItems);
    return modelBuilder.GetEdmModel();
}

/// <summary>Нужен для WebApplicationFactory в интеграционных тестах.</summary>
public partial class Program;
