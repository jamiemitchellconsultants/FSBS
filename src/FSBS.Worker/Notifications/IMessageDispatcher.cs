namespace FSBS.Worker.Notifications;

/// <summary>
/// Resolves the correct <see cref="INotificationHandler{T}"/> for a given
/// SQS <c>MessageType</c> attribute value and invokes it.
/// </summary>
public interface IMessageDispatcher
{
    Task DispatchAsync(string messageType, string messageBody, CancellationToken ct = default);
}
