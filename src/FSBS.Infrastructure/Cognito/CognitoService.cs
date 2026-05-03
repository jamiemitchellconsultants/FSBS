using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FSBS.Infrastructure.Cognito;

/// <summary>
/// AWS Cognito implementation of <see cref="ICognitoService"/>.
/// Catches Cognito-specific exceptions and re-throws domain exceptions so that
/// the Application layer and the API exception handler remain decoupled from
/// the AWS SDK.
/// </summary>
public sealed class CognitoService(
    IAmazonCognitoIdentityProvider client,
    IOptions<CognitoSettings> options)
    : ICognitoService
{
    private readonly CognitoSettings _settings = options.Value;

    /// <inheritdoc/>
    public async Task SignUpPrivateCustomerAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken ct = default)
    {
        var attributes = new List<AttributeType>
        {
            new() { Name = "email",         Value = email },
            new() { Name = "given_name",    Value = firstName },
            new() { Name = "family_name",   Value = lastName },
            // Signals the Pre Sign-up Lambda that no invitation token is needed.
            new() { Name = "custom:registration_type", Value = "private" },
        };

        if (phoneNumber is not null)
            attributes.Add(new AttributeType { Name = "phone_number", Value = phoneNumber });

        try
        {
            await client.SignUpAsync(new SignUpRequest
            {
                ClientId   = _settings.CustomerPoolClientId,
                Username   = email,
                Password   = password,
                UserAttributes = attributes,
            }, ct);
        }
        catch (UsernameExistsException)
        {
            throw new RegistrationEmailAlreadyExistsException(email);
        }
        catch (InvalidPasswordException ex)
        {
            // Surface Cognito's password policy message directly — it is already
            // human-readable and avoids duplicating the policy in application code.
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Password", ex.Message)]);
        }
    }

    /// <inheritdoc/>
    public async Task ConfirmSignUpAsync(
        string email,
        string confirmationCode,
        CancellationToken ct = default)
    {
        try
        {
            await client.ConfirmSignUpAsync(new ConfirmSignUpRequest
            {
                ClientId         = _settings.CustomerPoolClientId,
                Username         = email,
                ConfirmationCode = confirmationCode,
            }, ct);
        }
        catch (CodeMismatchException)
        {
            throw new InvalidConfirmationCodeException();
        }
        catch (ExpiredCodeException)
        {
            throw new ConfirmationCodeExpiredException();
        }
        catch (UserNotFoundException)
        {
            // Do not reveal whether the email exists — return the same error as
            // an incorrect code to prevent user enumeration.
            throw new InvalidConfirmationCodeException();
        }
    }

    /// <inheritdoc/>
    public async Task ResendConfirmationCodeAsync(
        string email,
        CancellationToken ct = default)
    {
        try
        {
            await client.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
            {
                ClientId = _settings.CustomerPoolClientId,
                Username = email,
            }, ct);
        }
        catch (UserNotFoundException)
        {
            // Silently succeed — revealing whether an email is registered would
            // allow user enumeration against the customer pool.
        }
    }
}
