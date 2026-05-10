using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Services;
using FSBS.Domain.Enums;
using FSBS.Shared.InstructorSchedule;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Queries;

/// <summary>
/// Returns the resolved schedule for an instructor across the supplied date
/// window. Self-access is permitted; cross-instructor reads require
/// <see cref="AppRole.SystemAdmin"/> or <see cref="AppRole.ScheduleAdmin"/>.
/// </summary>
public record GetInstructorScheduleQuery(Guid InstructorId, DateOnly From, DateOnly To)
    : IRequest<InstructorScheduleDto>;

public sealed class GetInstructorScheduleValidator : AbstractValidator<GetInstructorScheduleQuery>
{
    public GetInstructorScheduleValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.From).NotEqual(default(DateOnly));
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x).Must(q => q.To.DayNumber - q.From.DayNumber <= 92)
            .WithMessage("Date window must not exceed 92 days.");
    }
}

public sealed class GetInstructorScheduleHandler(
    IInstructorScheduleRepository repo,
    ICurrentUser currentUser)
    : IRequestHandler<GetInstructorScheduleQuery, InstructorScheduleDto>
{
    public async Task<InstructorScheduleDto> Handle(GetInstructorScheduleQuery request, CancellationToken ct)
    {
        await EnsureAuthorisedAsync(request.InstructorId, repo, currentUser, ct);

        var pattern = await repo.GetActivePatternAsync(request.InstructorId, ct);

        var fromUtc = new DateTimeOffset(request.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var toUtc = new DateTimeOffset(request.To.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(2);
        var overrides = await repo.GetOverridesAsync(request.InstructorId, fromUtc, toUtc, ct);

        var effective = InstructorScheduleResolver.Resolve(pattern, overrides, request.From, request.To);

        var patternDto = pattern is null
            ? null
            : new WeeklyPatternDto(
                pattern.Id,
                pattern.EffectiveFrom,
                pattern.Slots
                    .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                    .Select(s => new WeeklyPatternSlotDto(s.DayOfWeek, s.StartTime, s.EndTime))
                    .ToList());

        var overrideDtos = overrides
            .OrderBy(o => o.StartAt)
            .Select(o => new AvailabilityOverrideDto(o.Id, o.StartAt, o.EndAt, o.AvailabilityType.ToString(), o.Notes))
            .ToList();

        return new InstructorScheduleDto(request.InstructorId, request.From, request.To, patternDto, overrideDtos, effective);
    }

    internal static async Task EnsureAuthorisedAsync(
        Guid instructorId,
        IInstructorScheduleRepository repo,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.Role is AppRole.SystemAdmin or AppRole.ScheduleAdmin)
            return;

        var ownInstructorId = await repo.GetInstructorIdForUserAsync(currentUser.UserId, ct);
        if (ownInstructorId == instructorId)
            return;

        throw new ForbiddenException("You do not have access to this instructor's schedule.");
    }
}
