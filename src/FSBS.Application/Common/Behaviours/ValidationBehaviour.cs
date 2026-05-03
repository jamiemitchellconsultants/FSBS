using FluentValidation;
using MediatR;

namespace FSBS.Application.Common.Behaviours;

/// <summary>
/// Second pipeline behaviour. Runs all <see cref="IValidator{T}"/> instances
/// registered for <typeparamref name="TRequest"/> and throws a
/// <see cref="ValidationException"/> if any rules fail. Handlers only execute
/// when the request is fully valid.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // MediatR 12: RequestHandlerDelegate<T> is Func<Task<T>> — no CancellationToken parameter.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
