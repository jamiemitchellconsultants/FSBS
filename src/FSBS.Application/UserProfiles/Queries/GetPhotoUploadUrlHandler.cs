using FSBS.Application.Common.Interfaces;
using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Application.UserProfiles.Queries;

public sealed class GetPhotoUploadUrlHandler(ICurrentUser currentUser, IS3Service s3)
    : IRequestHandler<GetPhotoUploadUrlQuery, PhotoUploadUrlResponse>
{
    public async Task<PhotoUploadUrlResponse> Handle(GetPhotoUploadUrlQuery request, CancellationToken ct)
    {
        var ext = request.ContentType switch
        {
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg"
        };

        var objectKey = $"profile-photos/{currentUser.UserId}/{Guid.NewGuid()}.{ext}";
        var uploadUrl = await s3.GeneratePresignedPutUrlAsync(objectKey, request.ContentType, expiryMinutes: 10, ct);

        return new PhotoUploadUrlResponse(uploadUrl, objectKey);
    }
}
