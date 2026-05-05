namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Publishes domain event envelopes to an Amazon SQS queue for async
/// processing by the notification worker.
/// </summary>
public interface ISqsPublisher
{
    /// <summary>
    /// Serialises <paramref name="message"/> as JSON and sends it to the
    /// configured queue. The message type name is included as a message
    /// attribute so the worker can dispatch without deserialising the body.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
