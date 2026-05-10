namespace FSBS.Infrastructure.Storage;

public sealed class S3Settings
{
    /// <summary>Name of the private documents bucket (pre-signed URLs only).</summary>
    public string DocumentsBucketName { get; set; } = string.Empty;

    /// <summary>
    /// Optional override endpoint URL. Set to http://localhost:4566 when using
    /// LocalStack for local development. Leave empty in production.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Must be true when using LocalStack or any path-style S3-compatible service.
    /// Leave false (default) for real AWS S3.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}
