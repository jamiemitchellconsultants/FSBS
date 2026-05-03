using FSBS.Application.Invitations.Commands;
using FSBS.Application.Invitations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Endpoints;

public static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/invitations")
            .WithTags("Invitations")
            .RequireAuthorization();

        group.MapGet("/validate", ValidateAsync)
            .WithName("ValidateInvitationToken")
            .WithSummary("Validate a raw invitation token before showing the registration form.")
            .AllowAnonymous()
            .Produces<ValidateInvitationTokenResponse>(StatusCodes.Status200OK);

        group.MapPost("/", IssueAsync)
            .WithName("IssueCorporateManagerInvitation")
            .WithSummary("Issue a CorporateManager invitation (SalesStaff / SystemAdmin only).")
            .Produces<IssueCorporateManagerInvitationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/students", InviteStudentAsync)
            .WithName("InviteCorporateStudent")
            .WithSummary("Invite a CorporateStudent into the caller's own organisation (CorporateManager only).")
            .Produces<IssueCorporateManagerInvitationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ValidateAsync(
        [AsParameters] ValidateInvitationTokenRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ValidateInvitationTokenQuery(request.Token ?? ""), ct);
        return Results.Ok(new ValidateInvitationTokenResponse(
            result.IsValid, result.InviteeEmail, result.OrgName, result.Role));
    }

    private static async Task<IResult> InviteStudentAsync(
        [FromBody] InviteCorporateStudentRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new InviteCorporateStudentCommand(request.InviteeEmail),
            ct);

        var response = new IssueCorporateManagerInvitationResponse(
            result.InvitationId,
            result.InviteeEmail,
            result.OrgName,
            result.ExpiresAt);

        return Results.Created($"/v1/invitations/{result.InvitationId}", response);
    }

    private static async Task<IResult> IssueAsync(
        [FromBody] IssueCorporateManagerInvitationRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateCorporateManagerInvitationCommand(request.InviteeEmail, request.OrgId),
            ct);

        var response = new IssueCorporateManagerInvitationResponse(
            result.InvitationId,
            result.InviteeEmail,
            result.OrgName,
            result.ExpiresAt);

        return Results.Created($"/v1/invitations/{result.InvitationId}", response);
    }
}

public record IssueCorporateManagerInvitationRequest(string InviteeEmail, Guid OrgId);

public record InviteCorporateStudentRequest(string InviteeEmail);

public record ValidateInvitationTokenRequest(string? Token);

public record ValidateInvitationTokenResponse(
    bool IsValid,
    string? InviteeEmail,
    string? OrgName,
    string? Role);

public record IssueCorporateManagerInvitationResponse(
    Guid InvitationId,
    string InviteeEmail,
    string OrgName,
    DateTimeOffset ExpiresAt);
