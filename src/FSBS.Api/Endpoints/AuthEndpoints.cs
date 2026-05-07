using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return Results.Redirect($"/?auth_error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrWhiteSpace(code))
            return Results.Redirect("/?auth_error=missing_code");

        // Determine which pool this callback is for via the state parameter prefix.
        // state format: "staff|<random>" or "customer|<random>"
        var isStaff = state?.StartsWith("staff|", StringComparison.OrdinalIgnoreCase) ?? false;

        var awsRegion    = config["Cognito:AwsRegion"] ?? string.Empty;
        var poolId       = isStaff ? config["Cognito:StaffPoolId"]    : config["Cognito:CustomerPoolId"];
        var clientId     = isStaff ? config["Cognito:StaffClientId"]  : config["Cognito:CustomerClientId"];
        var clientSecret = isStaff ? config["Cognito:StaffClientSecret"] : config["Cognito:CustomerClientSecret"];
        var redirectUri  = isStaff ? config["Cognito:StaffCallbackUrl"]  : config["Cognito:CustomerCallbackUrl"];

        if (string.IsNullOrWhiteSpace(poolId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            return Results.Redirect("/?auth_error=misconfigured");

        var tokenEndpoint = $"https://{config["Cognito:Domain"]}.auth.{awsRegion}.amazoncognito.com/oauth2/token";

        var formValues = new Dictionary<string, string>
        {
            ["grant_type"]   = "authorization_code",
            ["client_id"]    = clientId,
            ["code"]         = code,
            ["redirect_uri"] = redirectUri,
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
            formValues["client_secret"] = clientSecret;

        using var client = httpClientFactory.CreateClient();
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await client.SendAsync(tokenRequest, ct);
        }
        catch
        {
            return Results.Redirect("/?auth_error=token_exchange_failed");
        }

        if (!tokenResponse.IsSuccessStatusCode)
            return Results.Redirect("/?auth_error=token_exchange_failed");

        var json = await tokenResponse.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var idToken      = doc.RootElement.TryGetProperty("id_token",      out var idTok)      ? idTok.GetString()      : null;
        var accessToken  = doc.RootElement.TryGetProperty("access_token",  out var accTok)     ? accTok.GetString()     : null;
        var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var refTok)     ? refTok.GetString()     : null;
        var expiresIn    = doc.RootElement.TryGetProperty("expires_in",    out var expIn)      ? expIn.GetInt32()       : 3600;

        if (string.IsNullOrWhiteSpace(idToken))
            return Results.Redirect("/?auth_error=no_id_token");

        var cookieOptions = new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Lax,
            Expires   = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            Path      = "/",
        };

        context.Response.Cookies.Append("fsbs_id_token", idToken, cookieOptions);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            context.Response.Cookies.Append("fsbs_refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Expires  = DateTimeOffset.UtcNow.AddDays(30),
                Path     = "/v1/auth",
            });
        }

        return Results.Redirect("/availability");
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
