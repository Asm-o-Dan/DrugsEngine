namespace Application.Interfaces.Repositories.BaseRepositories;

public interface IReadRepository<T> where T : class
{
    /// <summary>
    /// Получение сущности по идентификатору
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получение IQueryable для фильтрации и пагинации
    /// </summary>
    Task<IQueryable<T>> GetQueryableAsync(CancellationToken cancellationToken = default);
}