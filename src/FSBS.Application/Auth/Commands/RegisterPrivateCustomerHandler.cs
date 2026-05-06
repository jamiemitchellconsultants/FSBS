using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.Auth.Commands;

public sealed class RegisterPrivateCustomerHandler(ICognitoService cognito)
    : IRequestHandler<RegisterPrivateCustomerCommand>
{
    /// <inheritdoc/>
    public async Task Handle(RegisterPrivateCustomerCommand command, CancellationToken ct) =>
        await cognito.SignUpPrivateCustomerAsync(
            command.Email,
            command.Password,
            command.FirstName,
            command.LastName,
            command.PhoneNumber,
            ct);
}
