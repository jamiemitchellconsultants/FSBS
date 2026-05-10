using System.Net.Http.Json;
using FSBS.Shared.UserProfiles;

namespace FSBS.Web.Services;

public sealed class UserProfileService(HttpClient http)
{
    public async Task<UserProfileDto?> GetMyProfileAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<UserProfileDto>("v1/profile", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpdateMyProfileAsync(UpdateUserProfileRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync("v1/profile", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PhotoUploadUrlResponse?> GetPhotoUploadUrlAsync(string contentType, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<PhotoUploadUrlResponse>(
            $"v1/profile/photo-upload-url?contentType={Uri.EscapeDataString(contentType)}", ct);
        return response;
    }

    /// <summary>
    /// Uploads the photo bytes directly to S3 using the pre-signed PUT URL.
    /// The API is never in the upload path — the browser sends bytes straight to S3.
    /// </summary>
    public async Task UploadPhotoToS3Async(string presignedUrl, Stream photoStream, string contentType, CancellationToken ct = default)
    {
        using var content = new StreamContent(photoStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var s3Client = new HttpClient();
        var response = await s3Client.PutAsync(presignedUrl, content, ct);
        response.EnsureSuccessStatusCode();
    }
}
