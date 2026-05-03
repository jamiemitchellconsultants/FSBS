using FSBS.Application.Common.Interfaces;

namespace FSBS.Infrastructure.Cognito;

/// <summary>
/// No-op <see cref="ICognitoService"/> used when <c>DevAuth:Enabled</c> is true.
/// All methods succeed silently so the registration flow can be exercised locally
/// without an AWS Cognito pool. The dev seed endpoint (<c>POST /dev/users/seed</c>)
/// replaces the Post-Confirmation Lambda for creating the database record.
/// </summary>
internal sealed class StubCognitoService : ICognitoService
{
    public Task SignUpPrivateCustomerAsync(
        string email, string password, string firstName, string lastName,
        string? phoneNumber, CancellationToken ct = default) => Task.CompletedTask;

    public Task ConfirmSignUpAsync(
        string email, string confirmationCode, CancellationToken ct = default) => Task.CompletedTask;

    public Task ResendConfirmationCodeAsync(
        string email, CancellationToken ct = default) => Task.CompletedTask;
}
