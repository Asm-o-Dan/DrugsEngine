using Application.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.API.Infrastructure;

/// <summary>Переводит доменные исключения в ответы RFC 7807 (application/problem+json).</summary>
public sealed class ProblemExceptionHandler(IProblemDetailsService problemDetails, ILogger<ProblemExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Некорректные данные"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Не найдено"),
            SemanticSearchUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Семантический поиск недоступен"),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (499, "Запрос отменён"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера"),
        };

        if (status >= 500 && status != StatusCodes.Status503ServiceUnavailable)
            logger.LogError(exception, "Необработанная ошибка {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("{Title}: {Message}", title, exception.Message);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // текст внутренних ошибок наружу не отдаём
                Detail = status < 500 || status == StatusCodes.Status503ServiceUnavailable ? exception.Message : null,
            },
        });
    }
}
