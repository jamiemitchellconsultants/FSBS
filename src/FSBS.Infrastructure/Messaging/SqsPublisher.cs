using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FSBS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FSBS.Infrastructure.Messaging;

/// <summary>
/// Amazon SQS implementation of <see cref="ISqsPublisher"/>.
/// Serialises the message as JSON and includes the CLR type name as a
/// message attribute so the notification worker can dispatch without
/// deserialising the body first.
/// </summary>
internal sealed class SqsPublisher(IAmazonSQS sqs, IOptions<SqsSettings> options) : ISqsPublisher
{
    private readonly SqsSettings _settings = options.Value;

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var body = JsonSerializer.Serialize(message);
        var request = new SendMessageRequest
        {
            QueueUrl    = _settings.BookingEventsQueueUrl,
            MessageBody = body,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["MessageType"] = new MessageAttributeValue
                {
                    DataType    = "String",
                    StringValue = typeof(T).Name
                }
            }
        };
        await sqs.SendMessageAsync(request, ct);
    }
}
