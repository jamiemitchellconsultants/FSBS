namespace FSBS.Shared.UserProfiles;

public sealed record UserProfileDto(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? LicenceNumber,
    DateOnly? LicenceExpiry,
    string? PhotoS3Key,
    string? PhotoUrl);

public sealed record UpdateUserProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? LicenceNumber,
    DateOnly? LicenceExpiry,
    string? PhotoS3Key);

public sealed record PhotoUploadUrlResponse(string UploadUrl, string ObjectKey);
