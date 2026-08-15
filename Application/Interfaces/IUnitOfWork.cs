using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Сохраняет все изменения в базе данных
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество измененных записей</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Начинает новую транзакцию
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект транзакции</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Выполняет операцию в транзакции с автоматической обработкой ошибок и коммитом/откатом
    /// </summary>
    /// <typeparam name="TResult">Тип результата операции</typeparam>
    /// <param name="operation">Функция, описывающая операцию</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат выполнения операции</returns>
    Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Выполняет операцию в транзакции без возвращаемого значения
    /// </summary>
    /// <param name="operation">Функция, описывающая операцию</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ExecuteTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);
}