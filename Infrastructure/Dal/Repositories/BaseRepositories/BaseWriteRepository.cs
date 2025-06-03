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
        _dbContext.Set<T>().Update(entity);
        return Task.CompletedTask;
        // _dbContext.Set<T>().Attach(entity);
        // _dbContext.Entry(entity).State = EntityState.Modified;
        //TODO: имплементировать UnitOfWork
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