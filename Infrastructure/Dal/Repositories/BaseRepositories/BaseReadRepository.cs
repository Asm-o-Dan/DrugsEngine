using Application.Interfaces.Repositories.BaseRepositories;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Repositories.BaseRepositories;

public abstract class ReadRepository<T> : IReadRepository<T> where T : class
{
    protected readonly DrugsBotDbContext _dbContext;

    protected ReadRepository(DrugsBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public Task<IQueryable<T>> GetQueryableAsync(ODataQueryOptions<T> queryOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<T>()
            .AsNoTracking();
        var filteredQuery = queryOptions.ApplyTo(query, new ODataQuerySettings()) as IQueryable<T>;
        return Task.FromResult(filteredQuery!);
    }
}