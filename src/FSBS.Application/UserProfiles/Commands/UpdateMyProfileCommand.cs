using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Application.UserProfiles.Commands;

public record UpdateMyProfileCommand(UpdateUserProfileRequest Profile) : IRequest;
