using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Domain.Enums;
using FSBS.Shared.InstructorSchedule;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Commands;

/// <summary>
/// Creates or updates a single concrete availability override
/// (Available extra / Leave / Other). When <see cref="OverrideId"/> is null
/// a new override is created; otherwise the existing one is updated.
/// </summary>
public record UpsertOverrideCommand(
    Guid InstructorId,
    Guid? OverrideId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    AvailabilityType Type,
    string? Notes)
    : ICommand<AvailabilityOverrideDto>;

public sealed class UpsertOverrideValidator : AbstractValidator<UpsertOverrideCommand>
{
    public UpsertOverrideValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt);
        RuleFor(x => x).Must(c => (c.EndAt - c.StartAt).TotalDays <= 14)
            .WithMessage("An override cannot span more than 14 days.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpsertOverrideHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
    : IRequestHandler<UpsertOverrideCommand, AvailabilityOverrideDto>
{
    public async Task<AvailabilityOverrideDto> Handle(UpsertOverrideCommand request, CancellationToken ct)
    {
        await GetInstructorScheduleHandler.EnsureAuthorisedAsync(request.InstructorId, repo, currentUser, ct);

        var entity = request.OverrideId is { } id
            ? await repo.UpdateOverrideAsync(request.InstructorId, id, request.StartAt.ToUniversalTime(), request.EndAt.ToUniversalTime(), request.Type, request.Notes, ct)
            : await repo.AddOverrideAsync(request.InstructorId, request.StartAt.ToUniversalTime(), request.EndAt.ToUniversalTime(), request.Type, request.Notes, ct);

        return new AvailabilityOverrideDto(entity.Id, entity.StartAt, entity.EndAt, entity.AvailabilityType.ToString(), entity.Notes);
    }
}
