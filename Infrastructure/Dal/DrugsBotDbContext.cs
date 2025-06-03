using System.Reflection;
using Domain.Entities;
using Infrastructure.Dal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Dal;

/// <summary>
/// Контекст базы данных
/// Наследуется от <see cref="DbContext"/> и предоставляет доступ к таблицам базы данных через DbSet для каждой сущности.
/// </summary>
public class DrugsBotDbContext:DbContext
{
    private readonly DatabaseSettings _options;

    public DrugsBotDbContext(IOptions<DatabaseSettings> options)
    {
        _options = options.Value;
    }

    public DrugsBotDbContext()
    {
        
    }
    
    /// <summary>
    /// Таблица для хранения информации о препаратах.
    /// </summary>
    public DbSet<Drug?> Drugs { get; set; }

    /// <summary>
    /// Таблица для хранения информации об аптеках.
    /// </summary>
    public DbSet<DrugStore> DrugStores { get; set; }

    /// <summary>
    /// Таблица для хранения информации о препаратах в аптеках.
    /// </summary>
    public DbSet<DrugItem?> DrugItems { get; set; }

    /// <summary>
    /// Таблица для хранения информации о странах.
    /// </summary>
    public DbSet<Country> Countries { get; set; }

    /// <summary>
    /// Таблица для хранения профилей пользователей.
    /// </summary>
    public DbSet<UserProfile> Profiles { get; set; }

    /// <summary>
    /// Таблица для хранения избранных препаратов пользователей.
    /// </summary>
    public DbSet<FavoriteDrug> FavoriteDrugs { get; set; }

    /// <summary>
    /// Конфигурирует модели сущностей при создании базы данных, включая применение всех конфигураций из текущей сборки.
    /// </summary>
    /// <param name="modelBuilder">Объект, используемый для конфигурации моделей.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    /// <summary>
    /// Конфигурирует параметры подключения к базе данных.
    /// </summary>
    /// <param name="optionsBuilder">Объект, который используется для конфигурации параметров контекста базы данных.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
           optionsBuilder.UseNpgsql(_options.ConnectionString, (options) =>
           {
               options.CommandTimeout(_options.CommandTimeout);
           });
        }
    }
}