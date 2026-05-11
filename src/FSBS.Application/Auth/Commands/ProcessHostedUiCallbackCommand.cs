using MediatR;

namespace FSBS.Application.Auth.Commands;

public record ProcessHostedUiCallbackCommand(
    string? Code,
    string? State,
    string? Error) : IRequest<ProcessHostedUiCallbackResult>;

public record ProcessHostedUiCallbackResult(
    bool Success,
    string? ErrorCode,
    string? IdToken,
    string? RefreshToken,
    int ExpiresInSeconds);

