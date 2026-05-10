using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Application.InstructorSchedule.Services;
using FSBS.Shared.InstructorSchedule;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Commands;

/// <summary>
/// Replaces the instructor's concrete <c>Available</c> overrides for a single
/// local-time date. Existing Leave/Other overrides on the same date are not
/// touched. Pattern coverage is unaffected.
/// </summary>
public record SetSingleDayCommand(
    Guid InstructorId,
    DateOnly Date,
    IReadOnlyList<TimeRangeDto> Available)
    : ICommand<Unit>;

public sealed class SetSingleDayValidator : AbstractValidator<SetSingleDayCommand>
{
    public SetSingleDayValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.Available).NotNull();
        RuleForEach(x => x.Available).ChildRules(s =>
        {
            s.RuleFor(y => y.StartTime).Must(UpsertWeeklyPatternValidator.IsHalfHourAligned)
                .WithMessage("Start time must be aligned to :00 or :30.");
            s.RuleFor(y => y.EndTime).Must(UpsertWeeklyPatternValidator.IsHalfHourAligned)
                .WithMessage("End time must be aligned to :00 or :30.");
            s.RuleFor(y => y).Must(y => y.EndTime > y.StartTime)
                .WithMessage("End time must be after start time.");
        });
    }
}

public sealed class SetSingleDayHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
    : IRequestHandler<SetSingleDayCommand, Unit>
{
    public async Task<Unit> Handle(SetSingleDayCommand request, CancellationToken ct)
    {
        await GetInstructorScheduleHandler.EnsureAuthorisedAsync(request.InstructorId, repo, currentUser, ct);

        var ranges = request.Available
            .Select(r => (r.StartTime, r.EndTime))
            .ToList();

        await repo.ReplaceDayAvailableOverridesAsync(
            request.InstructorId,
            request.Date,
            ranges,
            InstructorScheduleResolver.SchoolTimeZone,
            ct);

        return Unit.Value;
    }
}
