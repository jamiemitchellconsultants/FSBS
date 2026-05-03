using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.Auth.Commands;

public sealed class ResendConfirmationCodeHandler(ICognitoService cognito)
    : IRequestHandler<ResendConfirmationCodeCommand>
{
    public async Task Handle(ResendConfirmationCodeCommand command, CancellationToken ct) =>
        await cognito.ResendConfirmationCodeAsync(command.Email, ct);
}
