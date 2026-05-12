using FSBS.Application.LessonLibrary.Commands;
using FSBS.Application.LessonLibrary.Queries;
using FSBS.Domain.Enums;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the curriculum lesson library. Routes under
/// <c>/v1/lesson-templates</c> for CRUD + <c>/v1/modules/{moduleId}/lessons/from-template</c>
/// for attach-to-module. Authorisation is enforced via the
/// <c>RequireLessonLibraryReader</c> / <c>RequireLessonLibraryWriter</c> /
/// <c>RequireCourseAuthor</c> policies registered in <c>Program.cs</c>.
/// </summary>
public static class LessonTemplateEndpoints
{
    /// <summary>Maps all lesson-library routes onto the application.</summary>
    public static IEndpointRouteBuilder MapLessonTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/lesson-templates")
            .WithTags("LessonLibrary");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization("RequireLessonLibraryWriter")
            .WithName("CreateLessonTemplate")
            .WithSummary("Create a new lesson template (SystemAdmin / CourseDirector).")
            .Produces<LessonTemplateDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", ListAsync)
            .RequireAuthorization("RequireLessonLibraryReader")
            .WithName("ListLessonTemplates")
            .WithSummary("Cursor-paginated list of lesson templates.")
            .Produces<PagedResult<LessonTemplateListItemDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization("RequireLessonLibraryReader")
            .WithName("GetLessonTemplate")
            .WithSummary("Full detail of a single lesson template.")
            .Produces<LessonTemplateDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization("RequireLessonLibraryWriter")
            .WithName("UpdateLessonTemplate")
            .WithSummary("Update a lesson template (SystemAdmin / CourseDirector).")
            .Produces<LessonTemplateDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/active", SetActiveAsync)
            .RequireAuthorization("RequireLessonLibraryWriter")
            .WithName("SetLessonTemplateActive")
            .WithSummary("Retire or un-retire a lesson template.")
            .Produces<LessonTemplateDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .RequireAuthorization("RequireLessonLibraryWriter")
            .WithName("SoftDeleteLessonTemplate")
            .WithSummary("Soft-delete a lesson template (existing attached lessons unaffected).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/v1/modules/{moduleId:guid}/lessons/from-template", AttachAsync)
            .RequireAuthorization("RequireCourseAuthor")
            .WithTags("LessonLibrary")
            .WithName("AttachLessonTemplateToModule")
            .WithSummary("Copy a lesson template into a new Lesson on the given module.")
            .Produces<LessonDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateLessonTemplateRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new CreateLessonTemplateCommand(
            request.Title,
            request.Description,
            request.TrainingType,
            request.DefaultMinDurationMins,
            request.RequiresInstructor,
            request.IsMandatoryByDefault,
            request.Category), ct);

        return Results.Created($"/v1/lesson-templates/{dto.Id}", dto);
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] TrainingType? trainingType,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListLessonTemplatesQuery(
            trainingType, category, isActive, search, cursor, limit ?? 25), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetLessonTemplateByIdQuery(id), ct);
        return dto is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Lesson template not found")
            : Results.Ok(dto);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateLessonTemplateRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new UpdateLessonTemplateCommand(
            id,
            request.Title,
            request.Description,
            request.TrainingType,
            request.DefaultMinDurationMins,
            request.RequiresInstructor,
            request.IsMandatoryByDefault,
            request.Category), ct);

        return Results.Ok(dto);
    }

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        [FromBody] SetLessonTemplateActiveRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new SetLessonTemplateActiveCommand(id, request.IsActive), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new SoftDeleteLessonTemplateCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AttachAsync(
        Guid moduleId,
        [FromBody] AttachLessonToModuleRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new AttachLessonTemplateToModuleCommand(
            request.LessonTemplateId,
            moduleId,
            request.SequenceOrder,
            request.MinDurationMins,
            request.RequiresInstructor,
            request.IsMandatory), ct);

        return Results.Created($"/v1/modules/{moduleId}/lessons/{dto.Id}", dto);
    }
}
