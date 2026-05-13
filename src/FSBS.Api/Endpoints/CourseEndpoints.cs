using FSBS.Application.Courses.Commands;
using FSBS.Shared.Courses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the curriculum Course aggregate. Routes under
/// <c>/v1/courses</c>. Currently exposes only <c>POST /</c>; read endpoints
/// will be added in a future slice.
/// </summary>
public static class CourseEndpoints
{
    /// <summary>Maps the course routes onto the application.</summary>
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/courses")
            .WithTags("Courses")
            .RequireAuthorization("RequireCourseAuthor");

        group.MapPost("/", CreateAsync)
            .WithName("CreateCourse")
            .WithSummary("Create a Course with optional initial Modules (CourseDirector / SystemAdmin).")
            .Produces<CreateCourseResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateCourseRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateCourseCommand(
            request.Title,
            request.Description,
            request.RegulatoryFramework,
            request.TotalHours,
            request.TrainingType,
            request.IsActive,
            request.Modules
                .Select(m => new CreateCourseModuleInput(m.Title, m.SequenceOrder, m.Description))
                .ToList());

        var result = await sender.Send(command, ct);
        var response = new CreateCourseResponse(
            result.CourseId, result.Title, result.TrainingType, result.ModuleCount);
        return Results.Created($"/v1/courses/{result.CourseId}", response);
    }
}
