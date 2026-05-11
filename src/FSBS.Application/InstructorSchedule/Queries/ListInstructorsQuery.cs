using FSBS.Domain.Interfaces;
using FSBS.Shared.InstructorSchedule;
using MediatR;

namespace FSBS.Application.InstructorSchedule.Queries;

/// <summary>
/// Retrieves a roster of all active instructors with their employee numbers and contact information.
/// Requires Staff role (SystemAdmin, ScheduleAdmin, etc.).
/// </summary>
public record ListInstructorsQuery : IRequest<IReadOnlyList<InstructorRowDto>>;

public sealed class ListInstructorsHandler(IInstructorRepository repo)
    : IRequestHandler<ListInstructorsQuery, IReadOnlyList<InstructorRowDto>>
{
    public async Task<IReadOnlyList<InstructorRowDto>> Handle(ListInstructorsQuery request, CancellationToken ct)
    {
        var instructors = await repo.ListAllAsync(ct);
        return instructors
            .Select(i => new InstructorRowDto(
                i.Id,
                i.EmployeeNumber,
                i.User.Profile != null ? (i.User.Profile.FirstName + " " + i.User.Profile.LastName) : i.User.Email,
                i.User.Email))
            .ToList();
    }
}


