using Application.Interfaces.Repositories.BaseRepositories;
using ISO3166;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Repositories.BaseRepositories;

public abstract class WriteRepository<T>: IWriteRepository<T> where T : class
{
    private readonly DrugsBotDbContext _dbContext;

    protected WriteRepository(DrugsBotDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey != null)
            {
                var keyValues = primaryKey.Properties
                    .Select(p => entry.Property(p.Name).CurrentValue)
                    .ToArray();

                var trackedEntry = _dbContext.ChangeTracker.Entries<T>()
                    .FirstOrDefault(e => primaryKey.Properties
                        .Select(p => e.Property(p.Name).CurrentValue)
                        .SequenceEqual(keyValues));

                if (trackedEntry != null)
                {
                    trackedEntry.CurrentValues.SetValues(entity);
                    trackedEntry.State = EntityState.Modified;
                    return Task.CompletedTask;
                }
            }

            _dbContext.Set<T>().Attach(entity);
            entry.State = EntityState.Modified;
        }
        else if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException("Сущность с таким ключем не найдена");
        }

        _dbContext.Set<T>().Remove(entity);

    }
    
    
    public void Detach(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Detached;
    }
}