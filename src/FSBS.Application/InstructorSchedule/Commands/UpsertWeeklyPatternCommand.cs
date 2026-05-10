using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Shared.InstructorSchedule;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Commands;

/// <summary>
/// Replaces the instructor's currently-active weekly pattern. Closes the open
/// pattern (if any) with <c>EffectiveTo = today</c> and inserts a fresh
/// pattern with <c>EffectiveFrom = today</c>.
/// </summary>
public record UpsertWeeklyPatternCommand(
    Guid InstructorId,
    IReadOnlyList<WeeklyPatternSlotDto> Slots)
    : ICommand<WeeklyPatternDto>;

public sealed class UpsertWeeklyPatternValidator : AbstractValidator<UpsertWeeklyPatternCommand>
{
    public UpsertWeeklyPatternValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.Slots).NotNull();
        RuleForEach(x => x.Slots).ChildRules(s =>
        {
            s.RuleFor(y => y.StartTime).Must(IsHalfHourAligned)
                .WithMessage("Start time must be aligned to :00 or :30.");
            s.RuleFor(y => y.EndTime).Must(IsHalfHourAligned)
                .WithMessage("End time must be aligned to :00 or :30.");
            s.RuleFor(y => y).Must(y => y.EndTime > y.StartTime)
                .WithMessage("End time must be after start time.");
        });
    }

    internal static bool IsHalfHourAligned(TimeOnly t) =>
        t.Second == 0 && t.Millisecond == 0 && (t.Minute == 0 || t.Minute == 30);
}

public sealed class UpsertWeeklyPatternHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UpsertWeeklyPatternCommand, WeeklyPatternDto>
{
    public async Task<WeeklyPatternDto> Handle(UpsertWeeklyPatternCommand request, CancellationToken ct)
    {
        await GetInstructorScheduleHandler.EnsureAuthorisedAsync(request.InstructorId, repo, currentUser, ct);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var slots = request.Slots
            .Select(s => (s.DayOfWeek, s.StartTime, s.EndTime))
            .ToList();

        var saved = await repo.ReplaceActivePatternAsync(request.InstructorId, slots, asOf, ct);

        return new WeeklyPatternDto(
            saved.Id,
            saved.EffectiveFrom,
            saved.Slots
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .Select(s => new WeeklyPatternSlotDto(s.DayOfWeek, s.StartTime, s.EndTime))
                .ToList());
    }
}
