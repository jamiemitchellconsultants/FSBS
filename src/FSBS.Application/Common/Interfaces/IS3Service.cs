namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Generates pre-signed URLs for the private documents S3 bucket.
/// The bucket is never served through CloudFront — all access is via
/// time-limited signed URLs only.
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Returns a pre-signed GET URL for the given object key that expires
    /// after <paramref name="expiryMinutes"/> minutes.
    /// </summary>
    Task<string> GeneratePresignedGetUrlAsync(
        string objectKey,
        int expiryMinutes = 15,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a pre-signed PUT URL allowing a client to upload directly to S3
    /// without routing the binary through the API.
    /// </summary>
    Task<string> GeneratePresignedPutUrlAsync(
        string objectKey,
        string contentType,
        int expiryMinutes = 15,
        CancellationToken ct = default);
}
