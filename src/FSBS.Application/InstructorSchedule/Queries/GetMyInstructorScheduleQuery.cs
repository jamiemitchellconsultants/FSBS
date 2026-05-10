using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Shared.InstructorSchedule;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Queries;

/// <summary>
/// Resolves the current user's instructor record (must hold the Instructor
/// role) and returns their schedule for the requested window. Throws
/// <see cref="ForbiddenException"/> if the caller is not an instructor.
/// </summary>
public record GetMyInstructorScheduleQuery(DateOnly From, DateOnly To)
    : IRequest<InstructorScheduleDto>;

public sealed class GetMyInstructorScheduleValidator : AbstractValidator<GetMyInstructorScheduleQuery>
{
    public GetMyInstructorScheduleValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly));
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x).Must(q => q.To.DayNumber - q.From.DayNumber <= 92)
            .WithMessage("Date window must not exceed 92 days.");
    }
}

public sealed class GetMyInstructorScheduleHandler(
    IInstructorScheduleRepository repo,
    ICurrentUser currentUser,
    ISender sender)
    : IRequestHandler<GetMyInstructorScheduleQuery, InstructorScheduleDto>
{
    public async Task<InstructorScheduleDto> Handle(GetMyInstructorScheduleQuery request, CancellationToken ct)
    {
        var instructorId = await repo.GetInstructorIdForUserAsync(currentUser.UserId, ct)
            ?? throw new ForbiddenException("Current user is not registered as an instructor.");

        return await sender.Send(new GetInstructorScheduleQuery(instructorId, request.From, request.To), ct);
    }
}
