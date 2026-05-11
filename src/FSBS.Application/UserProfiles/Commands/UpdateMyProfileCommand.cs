using FSBS.Application.Common.Interfaces;
using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Application.UserProfiles.Commands;

public record UpdateMyProfileCommand(UpdateUserProfileRequest Profile) : ICommand<Unit>;
