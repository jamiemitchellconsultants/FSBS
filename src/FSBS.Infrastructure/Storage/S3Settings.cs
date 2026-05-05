namespace FSBS.Infrastructure.Storage;

public sealed class S3Settings
{
    /// <summary>Name of the private documents bucket (pre-signed URLs only).</summary>
    public string DocumentsBucketName { get; set; } = string.Empty;
}
