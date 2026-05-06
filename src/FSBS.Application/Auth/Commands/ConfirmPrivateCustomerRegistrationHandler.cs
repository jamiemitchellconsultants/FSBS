using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.Auth.Commands;

public sealed class ConfirmPrivateCustomerRegistrationHandler(ICognitoService cognito)
    : IRequestHandler<ConfirmPrivateCustomerRegistrationCommand>
{
    /// <inheritdoc/>
    public async Task Handle(ConfirmPrivateCustomerRegistrationCommand command, CancellationToken ct) =>
        await cognito.ConfirmSignUpAsync(command.Email, command.ConfirmationCode, ct);
}
