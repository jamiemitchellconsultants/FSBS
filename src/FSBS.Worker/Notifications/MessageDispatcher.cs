using System.Text.Json;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications;

/// <summary>
/// Routes an SQS message to the correct typed <see cref="INotificationHandler{T}"/>
/// based on the <c>MessageType</c> attribute set by <c>SqsPublisher</c>.
/// </summary>
internal sealed class MessageDispatcher(IServiceProvider services, ILogger<MessageDispatcher> logger)
    : IMessageDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task DispatchAsync(string messageType, string messageBody, CancellationToken ct = default) =>
        messageType switch
        {
            nameof(SlotBookedEvent)         => Dispatch<SlotBookedEvent>(messageBody, ct),
            nameof(BookingConfirmedEvent)   => Dispatch<BookingConfirmedEvent>(messageBody, ct),
            nameof(BookingApprovedEvent)    => Dispatch<BookingApprovedEvent>(messageBody, ct),
            nameof(BookingRejectedEvent)    => Dispatch<BookingRejectedEvent>(messageBody, ct),
            nameof(BookingCancelledEvent)   => Dispatch<BookingCancelledEvent>(messageBody, ct),
            nameof(InvitationClaimedEvent)  => Dispatch<InvitationClaimedEvent>(messageBody, ct),
            _ => UnknownTypeAsync(messageType)
        };

    private async Task Dispatch<T>(string body, CancellationToken ct) where T : class
    {
        var message = JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialise {typeof(T).Name} from SQS body.");

        var handler = services.GetService<INotificationHandler<T>>();
        if (handler is null)
        {
            logger.LogWarning("No handler registered for {MessageType} — message acknowledged and discarded.", typeof(T).Name);
            return;
        }

        await handler.HandleAsync(message, ct);
    }

    private Task UnknownTypeAsync(string messageType)
    {
        logger.LogWarning("Unknown MessageType '{MessageType}' — message acknowledged and discarded.", messageType);
        return Task.CompletedTask;
    }
}
