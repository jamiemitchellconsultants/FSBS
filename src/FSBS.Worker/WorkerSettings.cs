namespace FSBS.Worker;

/// <summary>
/// Configuration settings for the FSBS notification worker.
/// Bound from the "Worker" section of appsettings.json / environment variables.
/// </summary>
public sealed class WorkerSettings
{
    /// <summary>URL of the SQS queue that receives booking domain events.</summary>
    public string BookingEventsQueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// SalesStaff distribution email address used for PendingApproval alerts.
    /// Defaults to a placeholder; override via environment variable in ECS.
    /// </summary>
    public string SalesStaffEmail { get; set; } = "salesstaff@fsbs.example.com";
}
