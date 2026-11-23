using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal;

/// <summary>
/// Контекст базы данных. Подключение настраивается в Program.cs (AddDbContext)
/// или в <see cref="DesignTimeDbContextFactory"/> для инструментов dotnet ef.
/// </summary>
public class DrugsBotDbContext(DbContextOptions<DrugsBotDbContext> options) : DbContext(options)
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<DrugStore> DrugStores => Set<DrugStore>();
    public DbSet<DrugItem> DrugItems => Set<DrugItem>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<FavoriteDrug> FavoriteDrugs => Set<FavoriteDrug>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
