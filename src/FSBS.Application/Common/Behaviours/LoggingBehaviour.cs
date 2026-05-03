using MediatR;
using Microsoft.Extensions.Logging;

namespace FSBS.Application.Common.Behaviours;

/// <summary>
/// First pipeline behaviour. Logs the request name on entry and exit so that
/// every command and query appears in structured logs without handler code
/// needing to repeat the boilerplate.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // MediatR 12: RequestHandlerDelegate<T> is Func<Task<T>> — no CancellationToken parameter.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);
        var response = await next();
        logger.LogInformation("Handled {RequestName}", name);
        return response;
    }
}
