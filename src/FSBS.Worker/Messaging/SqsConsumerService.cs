using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FSBS.Worker.Notifications;
using Microsoft.Extensions.Options;

namespace FSBS.Worker.Messaging;

/// <summary>
/// Long-running hosted service that polls the SQS booking events queue,
/// deserialises each message envelope, and dispatches to the appropriate
/// <see cref="INotificationHandler{T}"/> based on the <c>MessageType</c>
/// message attribute written by <c>SqsPublisher</c>.
/// </summary>
internal sealed class SqsConsumerService(
    IAmazonSQS sqs,
    IOptions<WorkerSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<SqsConsumerService> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer started. Queue: {QueueUrl}", _settings.BookingEventsQueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in SQS poll loop — backing off 5 s.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SQS consumer stopped.");
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl            = _settings.BookingEventsQueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds     = 20,   // long-poll
            MessageAttributeNames = ["MessageType"],
            MessageSystemAttributeNames = ["ApproximateReceiveCount"]
        }, ct);

        foreach (var message in response.Messages)
        {
            await ProcessMessageAsync(message, ct);
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        if (!message.MessageAttributes.TryGetValue("MessageType", out var typeAttr))
        {
            logger.LogWarning("Message {MessageId} has no MessageType attribute — skipping.", message.MessageId);
            await DeleteMessageAsync(message, ct);
            return;
        }

        var messageType = typeAttr.StringValue;
        logger.LogDebug("Dispatching message {MessageId} of type {MessageType}.", message.MessageId, messageType);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
            await dispatcher.DispatchAsync(messageType, message.Body, ct);
            await DeleteMessageAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message {MessageId} (type={MessageType}).", message.MessageId, messageType);
            // Leave on queue — SQS visibility timeout will return it for retry.
            // After maxReceiveCount the DLQ will capture it.
        }
    }

    private Task DeleteMessageAsync(Message message, CancellationToken ct) =>
        sqs.DeleteMessageAsync(_settings.BookingEventsQueueUrl, message.ReceiptHandle, ct);
}
