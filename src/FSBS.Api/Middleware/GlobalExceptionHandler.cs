using FSBS.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FSBS.Api.Middleware;

/// <summary>
/// Translates domain and validation exceptions into RFC 7807 Problem Details
/// responses. Registered via <c>app.UseExceptionHandler()</c>.
/// </summary>
/// <remarks>
/// Mapping table:
/// <list type="table">
///   <item><term><see cref="ValidationException"/></term><description>422 Unprocessable Entity — field-level errors</description></item>
///   <item><term><see cref="RegistrationEmailAlreadyExistsException"/></term><description>409 Conflict</description></item>
///   <item><term><see cref="InvalidConfirmationCodeException"/></term><description>400 Bad Request</description></item>
///   <item><term><see cref="ConfirmationCodeExpiredException"/></term><description>400 Bad Request</description></item>
///   <item><term>All others</term><description>500 Internal Server Error (detail suppressed)</description></item>
/// </list>
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, problem) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Title  = "Validation failed",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions =
                    {
                        ["errors"] = ve.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray())
                    }
                }),

            RegistrationEmailAlreadyExistsException rex => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title  = "Email already registered",
                    Detail = rex.Message,
                    Status = StatusCodes.Status409Conflict,
                }),

            DuplicateInvitationException dex => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title  = "Invitation already exists",
                    Detail = dex.Message,
                    Status = StatusCodes.Status409Conflict,
                }),

            OrganisationNotFoundException nfex => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title  = "Organisation not found",
                    Detail = nfex.Message,
                    Status = StatusCodes.Status404NotFound,
                }),

            InvitationNotFoundException infex => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title  = "Invitation not found",
                    Detail = infex.Message,
                    Status = StatusCodes.Status404NotFound,
                }),

            InvitationAlreadyClaimedException acex => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title  = "Invitation already claimed",
                    Detail = acex.Message,
                    Status = StatusCodes.Status409Conflict,
                }),

            BookingNotFoundException bnfex => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title  = "Booking not found",
                    Detail = bnfex.Message,
                    Status = StatusCodes.Status404NotFound,
                }),

            InvalidConfirmationCodeException or ConfirmationCodeExpiredException => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Title  = "Invalid confirmation code",
                    Detail = exception.Message,
                    Status = StatusCodes.Status400BadRequest,
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title  = "An unexpected error occurred",
                    Status = StatusCodes.Status500InternalServerError,
                })
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        problem.Type = $"https://httpstatuses.io/{statusCode}";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
