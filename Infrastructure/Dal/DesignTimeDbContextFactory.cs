using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Dal;

/// <summary>Для dotnet ef (миграции). Строка подключения — из ConnectionStrings__Default.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DrugsBotDbContext>
{
    public DrugsBotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=drugsengine;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<DrugsBotDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new DrugsBotDbContext(options);
    }
}
