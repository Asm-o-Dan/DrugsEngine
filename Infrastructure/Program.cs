using Infrastructure.Dal;
using Infrastructure.Dal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Domain.Entities;
using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Application.Interfaces.Repositories.DrugRepositories;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Application.UseCases.Commands.DrugCommands;
using Infrastructure.Dal.Repositories.CountryRepositories;
using Infrastructure.Dal.Repositories.DrugItemRepositories;
using Infrastructure.Dal.Repositories.DrugRepositories;
using Infrastructure.Dal.Repositories.DrugStoreRepositories;
using Infrastructure.Dal.Repositories.FavoriteDrugRepositories;
using Infrastructure.Dal.Repositories.UserProfileRepositories;
using Infrastructure.Kafka;
using Infrastructure.Parsing;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

#region Logging Configuration
//
// var logFilePath = Path.Combine("logs", $"{DateTime.Now:yyyy-MM-dd_HH-mm}.log");
//
// Log.Logger = new LoggerConfiguration()
//     .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
//     .WriteTo.File(
//         path: logFilePath,
//         outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
//         flushToDiskInterval: TimeSpan.FromSeconds(10),
//         rollingInterval: RollingInterval.Infinite,
//         buffered: false
//     )
//     .CreateLogger();
var logFilePath = Path.Combine("logs", $"{DateTime.Now:yyyy-MM-dd_HH-mm}.log");// Добавляем шаблон для rolling

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: logFilePath,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: 7,
        flushToDiskInterval: TimeSpan.FromSeconds(1)
    )
    .CreateLogger();
builder.Host.UseSerilog();
#endregion

#region OData Model Configuration

static IEdmModel GetEdmModel()
{
    var modelBuilder = new ODataConventionModelBuilder();
    var drugSet = modelBuilder.EntitySet<Drug>("Drug");
    drugSet.EntityType.HasKey(d => d.Id);
    drugSet.EntityType.Ignore(d => d.DrugItems);
    return modelBuilder.GetEdmModel();
}

#endregion

#region Dependency Injection

// Drug
builder.Services.AddScoped<IDrugWriteRepository, DrugWriteRepository>();
builder.Services.AddScoped<IDrugReadRepository, DrugReadRepository>();

// DrugStore
builder.Services.AddScoped<IDrugStoreWriteRepository, DrugStoreWriteRepository>();
builder.Services.AddScoped<IDrugStoreReadRepository, DrugStoreReadRepository>();

// DrugItem
builder.Services.AddScoped<IDrugItemWriteRepository, DrugItemWriteRepository>();
builder.Services.AddScoped<IDrugItemReadRepository, DrugItemReadRepository>();

// UserProfile
builder.Services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
builder.Services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();

// FavoriteDrug
builder.Services.AddScoped<IFavoriteDrugWriteRepository, FavoriteDrugWriteRepository>();
builder.Services.AddScoped<IFavoriteDrugReadRepository, FavoriteDrugReadRepository>();

// Country
builder.Services.AddScoped<ICountryWriteRepository, CountryWriteRepository>();
builder.Services.AddScoped<ICountryReadRepository, CountryReadRepository>();

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Parsers
builder.Services.AddSingleton<IPharmacyParser, DoctorParser>();
//builder.Services.AddSingleton<IPharmacyParser, VivaFarmParser>();
builder.Services.AddSingleton<ParsingManager>(sp =>
{
    var parsers = sp.GetServices<IPharmacyParser>().ToList();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    var logger = sp.GetService<ILogger<ParsingManager>>();
    return new ParsingManager(parsers, scopeFactory, logger);
});

//Kafka
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();

#endregion

#region Controllers & OData

builder.Services.AddControllers()
    .AddOData(options =>
        options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
            .AddRouteComponents("api", GetEdmModel())
    );

#endregion

#region MediatR

builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(CreateDrugCommand).Assembly); });

#endregion

#region Database Configuration

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection(nameof(DatabaseSettings))
);
builder.Services.AddDbContext<DrugsBotDbContext>(
    (serviceProvider, options) =>
    {
        var databaseSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        Console.WriteLine($"Using database: {databaseSettings.ConnectionString}");
        options.UseNpgsql(databaseSettings.ConnectionString,
                npgsqlOptions => npgsqlOptions.CommandTimeout(databaseSettings.CommandTimeout))
            .EnableSensitiveDataLogging(true).EnableDetailedErrors(true).EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information);;
    },
    ServiceLifetime.Scoped // Явно указываем Scoped
);

using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
    db.Database.Migrate(); // накатить миграции
}


#endregion

var app = builder.Build();

#region Middleware Configuration

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started at {Time}", DateTime.Now);


var pm = app.Services.GetService<ParsingManager>();
pm.ProcessAllPharmaciesAsync(CancellationToken.None).GetAwaiter().GetResult();


app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
try
{
    app.Run();
    Log.CloseAndFlush();
}
finally
{
    Log.CloseAndFlush();
}

#endregion