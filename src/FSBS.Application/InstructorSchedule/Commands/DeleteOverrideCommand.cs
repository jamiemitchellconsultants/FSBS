using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Queries;
using FluentValidation;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Commands;

/// <summary>Soft-deletes a single override that belongs to the given instructor.</summary>
public record DeleteOverrideCommand(Guid InstructorId, Guid OverrideId) : ICommand<Unit>;

public sealed class DeleteOverrideValidator : AbstractValidator<DeleteOverrideCommand>
{
    public DeleteOverrideValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.OverrideId).NotEmpty();
    }
}

public sealed class DeleteOverrideHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
    : IRequestHandler<DeleteOverrideCommand, Unit>
{
    public async Task<Unit> Handle(DeleteOverrideCommand request, CancellationToken ct)
    {
        await GetInstructorScheduleHandler.EnsureAuthorisedAsync(request.InstructorId, repo, currentUser, ct);
        await repo.DeleteOverrideAsync(request.InstructorId, request.OverrideId, ct);
        return Unit.Value;
    }
}
