using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Infrastructure.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private static readonly System.Text.RegularExpressions.Regex SafeCorrelationIdRegex =
        new("^[a-zA-Z0-9_-]{1,64}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public async Task InvokeAsync(HttpContext context)
    {
        var rawCorrelationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        string correlationId;

        if (!string.IsNullOrWhiteSpace(rawCorrelationId) && SafeCorrelationIdRegex.IsMatch(rawCorrelationId))
        {
            correlationId = rawCorrelationId;
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
