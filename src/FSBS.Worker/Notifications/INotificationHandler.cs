namespace FSBS.Worker.Notifications;

/// <summary>
/// Handles a specific notification event type deserialized from an SQS message.
/// </summary>
public interface INotificationHandler<in T> where T : class
{
    Task HandleAsync(T notification, CancellationToken ct = default);
}
