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
using Infrastructure.Middleware;
using Infrastructure.Parsing;
using Confluent.Kafka;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Infrastructure.Telemetry;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

#region Logging Configuration

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
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
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

#endregion

#region Controllers & OData

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        Log.Information("Configuring database with CommandTimeout={CommandTimeout}s", databaseSettings.CommandTimeout);
        options.UseNpgsql(databaseSettings.ConnectionString,
                npgsqlOptions => npgsqlOptions.CommandTimeout(databaseSettings.CommandTimeout));

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging(true)
                   .EnableDetailedErrors(true);
        }
    },
    ServiceLifetime.Scoped
);

#endregion

#region Health Checks

var connectionString = builder.Configuration.GetSection(nameof(DatabaseSettings)).GetValue<string>("ConnectionString")
    ?? "Host=db;Port=5432;Database=myappdb;Username=postgres;Password=postgres";

var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"];
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = builder.Configuration["Kafka:Broker"];
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = builder.Configuration["Kafka__BootstrapServers"];
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = builder.Configuration["Kafka__Broker"];
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = builder.Configuration["KAFKA_BROKER"];
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"];
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = Environment.GetEnvironmentVariable("Kafka__BootstrapServers");
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = Environment.GetEnvironmentVariable("Kafka__Broker");
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = Environment.GetEnvironmentVariable("KAFKA_BROKER");
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
}
if (string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    kafkaBootstrap = "kafka:9092";
}
kafkaBootstrap = kafkaBootstrap.Trim();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "db", "ready" })
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBootstrap, MessageTimeoutMs = 3000 }, name: "kafka", tags: new[] { "broker", "ready" });

#endregion

#region OpenTelemetry Configuration

var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DrugsEngineTelemetry.ServiceName)
        .AddSource(DrugsEngineTelemetry.ParsingActivitySource.Name)
        .AddSource(DrugsEngineTelemetry.KafkaActivitySource.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddMeter(DrugsEngineTelemetry.Meter.Name)
        .AddMeter(DrugsEngineTelemetry.ParsingMeter.Name)
        .AddMeter(DrugsEngineTelemetry.KafkaMeter.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

#endregion

var app = builder.Build();

#region Database Migration

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
    db.Database.Migrate();
}

#endregion

#region Middleware Configuration

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500)
        {
            Log.Error(exception, "Unhandled server error on {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
        else
        {
            Log.Warning("Client error {StatusCode} on {Method} {Path}: {Message}", 
                statusCode, context.Request.Method, context.Request.Path, exception?.Message);
        }
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == 404 ? "Resource Not Found" : (statusCode == 400 ? "Validation Error" : "An unhandled error occurred."),
            Detail = (app.Environment.IsDevelopment() || statusCode < 500) ? exception?.Message : "An unexpected error occurred processing your request.",
            Instance = context.Request.Path
        };
        
        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application initialized at {Time}", DateTime.Now);

// Запуск парсинга в фоновом режиме, чтобы не блокировать запуск веб-сервера и health check probes
_ = Task.Run(async () =>
{
    try
    {
        using var parsingScope = app.Services.CreateScope();
        var pm = parsingScope.ServiceProvider.GetRequiredService<ParsingManager>();
        await pm.ProcessAllPharmaciesAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Background parsing process encountered an unhandled error");
    }
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPrometheusScrapingEndpoint();

try
{
    Log.Information("Starting DrugsEngine Web Application...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DrugsEngine application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

#endregion