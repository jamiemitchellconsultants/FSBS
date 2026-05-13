using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSBS.Application.Courses.Commands;

/// <summary>
/// Handler for <see cref="CreateCourseCommand"/>. Builds the aggregate
/// (<c>Course</c> + child <c>Module</c>s), stamps <c>TenantId</c> from the
/// current user, and persists via <see cref="ICourseRepository.AddAsync"/>.
/// Audit columns are stamped automatically by the audit interceptor.
/// </summary>
public sealed class CreateCourseHandler(
    ICurrentUser currentUser,
    ICourseRepository courses,
    ILogger<CreateCourseHandler> logger)
    : IRequestHandler<CreateCourseCommand, CreateCourseResult>
{
    /// <inheritdoc/>
    public async Task<CreateCourseResult> Handle(CreateCourseCommand command, CancellationToken ct)
    {
        var course = new Course
        {
            Id                  = Guid.NewGuid(),
            TenantId            = currentUser.TenantId,
            Title               = command.Title,
            Description         = command.Description,
            RegulatoryFramework = command.RegulatoryFramework,
            TotalHours          = command.TotalHours,
            TrainingType        = command.TrainingType,
            IsActive            = command.IsActive,
        };

        foreach (var m in command.Modules)
        {
            course.Modules.Add(new Module
            {
                Id            = Guid.NewGuid(),
                CourseId      = course.Id,
                Title         = m.Title,
                SequenceOrder = m.SequenceOrder,
                Description   = m.Description,
            });
        }

        await courses.AddAsync(course, ct);

        logger.LogInformation(
            "Course {CourseId} created by {UserId} in tenant {TenantId} with {ModuleCount} modules",
            course.Id, currentUser.UserId, currentUser.TenantId, course.Modules.Count);

        return new CreateCourseResult(course.Id, course.Title, course.TrainingType, course.Modules.Count);
    }
}
