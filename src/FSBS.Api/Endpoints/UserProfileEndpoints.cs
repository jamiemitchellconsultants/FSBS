using FSBS.Application.UserProfiles.Commands;
using FSBS.Application.UserProfiles.Queries;
using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Api.Endpoints;

public static class UserProfileEndpoints
{
    public static IEndpointRouteBuilder MapUserProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", GetMyProfileAsync)
            .WithName("GetMyProfile")
            .WithSummary("Return the current user's profile.")
            .Produces<UserProfileDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateMyProfileAsync)
            .WithName("UpdateMyProfile")
            .WithSummary("Create or update the current user's profile.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/photo-upload-url", GetPhotoUploadUrlAsync)
            .WithName("GetPhotoUploadUrl")
            .WithSummary("Return a pre-signed S3 PUT URL for uploading a profile photo.")
            .Produces<PhotoUploadUrlResponse>();

        return app;
    }

    private static async Task<IResult> GetMyProfileAsync(ISender sender, CancellationToken ct)
    {
        var dto = await sender.Send(new GetMyProfileQuery(), ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        ISender sender,
        UpdateUserProfileRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.FirstName) || string.IsNullOrWhiteSpace(body.LastName))
            return Results.Problem("FirstName and LastName are required.",
                statusCode: StatusCodes.Status400BadRequest);

        await sender.Send(new UpdateMyProfileCommand(body), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetPhotoUploadUrlAsync(
        ISender sender,
        string contentType,
        CancellationToken ct)
    {
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(contentType))
            return Results.Problem("contentType must be image/jpeg, image/png, or image/webp.",
                statusCode: StatusCodes.Status400BadRequest);

        var result = await sender.Send(new GetPhotoUploadUrlQuery(contentType), ct);
        return Results.Ok(result);
    }
}
