using MediatR;

namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands. TransactionBehaviour constrains itself to
/// ICommand so that read-only queries are never wrapped in a DB transaction.
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>;
