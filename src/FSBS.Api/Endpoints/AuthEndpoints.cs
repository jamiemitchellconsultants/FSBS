using FSBS.Application.Auth.Commands;
using FSBS.Application.Invitations.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/auth")
            .WithTags("Auth")
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterPrivateCustomer")
            .WithSummary("Start private customer self-registration.")
            .WithDescription(
                "Creates a Cognito account and sends a 6-digit confirmation code to the " +
                "supplied email address. No invitation token is required. " +
                "Follow up with POST /v1/auth/register/confirm to complete registration.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/register/confirm", ConfirmAsync)
            .WithName("ConfirmPrivateCustomerRegistration")
            .WithSummary("Confirm registration with the emailed code.")
            .WithDescription(
                "Submits the 6-digit code sent by Cognito. On success the user's " +
                "AppUser and UserProfile records are created by the Post Confirmation Lambda " +
                "and the account is ready to sign in.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/register/resend-code", ResendCodeAsync)
            .WithName("ResendPrivateCustomerConfirmationCode")
            .WithSummary("Re-send the email confirmation code.")
            .WithDescription(
                "Asks Cognito to issue a new confirmation code. Use when the original " +
                "code has expired (24-hour TTL) or was not received.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPost("/register/invited", RegisterInvitedAsync)
            .WithName("RegisterInvitedUser")
            .WithSummary("Accept an invitation and create a CorporateManager or CorporateStudent account.")
            .WithDescription(
                "Validates the raw invitation token, creates AppUser + UserProfile + OrgMembership, " +
                "and marks the invitation as Claimed in a single atomic operation. " +
                "In production this is superseded by the Cognito Post-Confirmation Lambda.")
            .Produces<RegisterInvitedResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterPrivateCustomerRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new RegisterPrivateCustomerCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber), ct);

        return Results.Accepted();
    }

    private static async Task<IResult> ConfirmAsync(
        [FromBody] ConfirmRegistrationRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new ConfirmPrivateCustomerRegistrationCommand(request.Email, request.ConfirmationCode), ct);

        return Results.NoContent();
    }

    private static async Task<IResult> ResendCodeAsync(
        [FromBody] ResendConfirmationCodeRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new ResendConfirmationCodeCommand(request.Email), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RegisterInvitedAsync(
        [FromBody] RegisterInvitedRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ClaimInvitationCommand(
            request.Token,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber), ct);

        return Results.Ok(new RegisterInvitedResponse(
            result.UserId,
            result.Email,
            result.OrgId,
            result.Role));
    }
}

/// <summary>Request body for <c>POST /v1/auth/register</c>.</summary>
public record RegisterPrivateCustomerRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

/// <summary>Request body for <c>POST /v1/auth/register/confirm</c>.</summary>
public record ConfirmRegistrationRequest(
    string Email,
    string ConfirmationCode);

/// <summary>Request body for <c>POST /v1/auth/register/resend-code</c>.</summary>
public record ResendConfirmationCodeRequest(string Email);

/// <summary>Request body for <c>POST /v1/auth/register/invited</c>.</summary>
public record RegisterInvitedRequest(
    string Token,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public record RegisterInvitedResponse(
    Guid UserId,
    string Email,
    Guid OrgId,
    string Role);
