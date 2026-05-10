using FSBS.Shared.UserProfiles;
using MediatR;

namespace FSBS.Application.UserProfiles.Queries;

public record GetMyProfileQuery : IRequest<UserProfileDto?>;
