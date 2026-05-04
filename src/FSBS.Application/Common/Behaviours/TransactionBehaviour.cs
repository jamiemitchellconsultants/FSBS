using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSBS.Application.Common.Behaviours;

/// <summary>
/// Third pipeline behaviour. Applies only to ICommand requests. After the
/// handler executes it collects domain events from tracked aggregate roots,
/// flushes changes to the database via IUnitOfWork, then dispatches the events
/// so that side-effects (emails, cache invalidation, SignalR pushes) run after
/// the commit succeeds.
/// </summary>
public sealed class TransactionBehaviour<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher dispatcher,
    ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var response = await next();

        var events = unitOfWork.CollectAndClearDomainEvents();

        var changes = await unitOfWork.SaveChangesAsync(ct);
        logger.LogDebug(
            "{RequestName} committed {Changes} change(s) with {EventCount} domain event(s)",
            typeof(TRequest).Name, changes, events.Count);

        if (events.Count > 0)
            await dispatcher.DispatchAsync(events, ct);

        return response;
    }
}
