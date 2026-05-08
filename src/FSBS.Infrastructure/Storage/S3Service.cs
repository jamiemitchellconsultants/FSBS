using Amazon.S3;
using Amazon.S3.Model;
using FSBS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FSBS.Infrastructure.Storage;

/// <summary>
/// Amazon S3 implementation of <see cref="IS3Service"/>.
/// Only generates pre-signed URLs — the documents bucket is never served
/// through CloudFront directly.
/// </summary>
internal sealed class S3Service(
    IAmazonS3 s3,
    IOptions<S3Settings> options) : IS3Service
{
    private readonly S3Settings _settings = options.Value;

    /// <summary>
    /// Generates a pre-signed GET URL for the given S3 object key in the documents bucket.
    /// </summary>
    public Task<string> GeneratePresignedGetUrlAsync(
        string objectKey,
        int expiryMinutes = 15,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.DocumentsBucketName,
            Key        = objectKey,
            Verb       = HttpVerb.GET,
            Expires    = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
        return Task.FromResult(s3.GetPreSignedURL(request));
    }

    /// <summary>
    /// Generates a pre-signed PUT URL for uploading a document to the documents bucket.
    /// </summary>
    public Task<string> GeneratePresignedPutUrlAsync(
        string objectKey,
        string contentType,
        int expiryMinutes = 15,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName  = _settings.DocumentsBucketName,
            Key         = objectKey,
            Verb        = HttpVerb.PUT,
            ContentType = contentType,
            Expires     = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
        return Task.FromResult(s3.GetPreSignedURL(request));
    }
}
