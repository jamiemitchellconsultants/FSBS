using System.Security.Claims;
using FSBS.Application.Auth.Commands;
using FSBS.Application.Invitations.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for customer self-registration, confirmation, and session management.
/// All routes are under <c>/v1/auth</c> and are anonymous unless noted.
/// </summary>
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

        // ── Session endpoints ─────────────────────────────────────────────────

        // GET /v1/auth/me — returns the current user's identity from JWT claims.
        // Used by CognitoAuthStateProvider on every app load to rehydrate session.
        // Requires a valid Bearer token (dev JWT or Cognito JWT).
        app.MapGet("/v1/auth/me", MeAsync)
            .WithTags("Auth")
            .WithName("GetCurrentUser")
            .WithSummary("Return identity claims for the authenticated caller.")
            .Produces<MeResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        // GET /v1/auth/callback?code=...&state=... — production Cognito hosted UI callback.
        // Exchanges the authorisation code for tokens via Cognito's token endpoint,
        // sets an HttpOnly cookie containing the id_token, then redirects to the SPA.
        app.MapGet("/v1/auth/callback", CallbackAsync)
            .WithTags("Auth")
            .WithName("CognitoCallback")
            .WithSummary("Handle Cognito hosted UI redirect and exchange auth code for tokens.")
            .AllowAnonymous();

        // POST /v1/auth/logout — clears the session cookie.
        app.MapPost("/v1/auth/logout", LogoutAsync)
            .WithTags("Auth")
            .WithName("Logout")
            .WithSummary("Clear the session cookie.")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

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

    private static IResult MeAsync(ClaimsPrincipal user)
    {
        var sub      = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var appRole  = user.FindFirstValue("app_role") ?? string.Empty;
        var tenantId = user.FindFirstValue("tenant_id") ?? string.Empty;
        var orgId    = user.FindFirstValue("org_id");
        var email    = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name     = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        return Results.Ok(new MeResponse(sub, email, name, appRole, tenantId, orgId));
    }

    private static async Task<IResult> CallbackAsync(
        string? code,
        string? state,
        string? error,
        HttpContext context,
        ISender sender,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // If the session cookie is already present the browser is replaying the
        // callback (e.g. a second navigation from the browser history, or a
        // round-robin hit to a second ECS task after a successful exchange on the
        // first). Skip re-processing and hand off to the SPA callback page which
        // will verify the existing session and navigate to /availability.
        if (!string.IsNullOrWhiteSpace(context.Request.Cookies["fsbs_id_token"]))
            return Results.Redirect("/auth/callback");

        if (!string.IsNullOrWhiteSpace(error))
        {
            var desc = context.Request.Query["error_description"].FirstOrDefault();
            logger.LogWarning("Cognito callback error={Error} error_description={ErrorDescription}", error, desc);
        }

        var callback = await sender.Send(new ProcessHostedUiCallbackCommand(code, state, error), ct);

        if (!callback.Success)
            return Results.Redirect($"/auth/callback?auth_error={Uri.EscapeDataString(callback.ErrorCode ?? "token_exchange_failed")}");

        var cookieOptions = new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Lax,
            Expires   = DateTimeOffset.UtcNow.AddSeconds(callback.ExpiresInSeconds),
            Path      = "/",
        };

        context.Response.Cookies.Append("fsbs_id_token", callback.IdToken!, cookieOptions);

        if (!string.IsNullOrWhiteSpace(callback.RefreshToken))
        {
            context.Response.Cookies.Append("fsbs_refresh_token", callback.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Expires  = DateTimeOffset.UtcNow.AddDays(30),
                Path     = "/v1/auth",
            });
        }

        // Redirect to the SPA callback page, which calls NotifyAuthChanged() and
        // verifies the new session before navigating the user to /availability.
        return Results.Redirect("/auth/callback");
    }

    private static IResult LogoutAsync(HttpContext context)
    {
        context.Response.Cookies.Delete("fsbs_id_token");
        context.Response.Cookies.Delete("fsbs_refresh_token");
        return Results.NoContent();
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

/// <summary>Response body for <c>POST /v1/auth/register/invited</c>.</summary>
public record RegisterInvitedResponse(
    Guid UserId,
    string Email,
    Guid OrgId,
    string Role);

/// <summary>Response body for <c>GET /v1/auth/me</c>.</summary>
public record MeResponse(
    string Sub,
    string Email,
    string Name,
    string AppRole,
    string TenantId,
    string? OrgId);
