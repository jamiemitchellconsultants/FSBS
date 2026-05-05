namespace FSBS.Infrastructure.Messaging;

public sealed class SqsSettings
{
    /// <summary>URL of the SQS queue that receives booking domain events.</summary>
    public string BookingEventsQueueUrl { get; set; } = string.Empty;
}
