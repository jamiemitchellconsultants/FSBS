using System.Security.Cryptography;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Invitations.Queries;

public sealed class ValidateInvitationTokenHandler(IInvitationRepository invitations)
    : IRequestHandler<ValidateInvitationTokenQuery, ValidateInvitationTokenResult>
{
    private static readonly ValidateInvitationTokenResult Invalid = new(false, null, null, null);

    /// <inheritdoc/>
    public async Task<ValidateInvitationTokenResult> Handle(
        ValidateInvitationTokenQuery request,
        CancellationToken ct)
    {
        if (!TryHashToken(request.Token, out var tokenHash))
            return Invalid;

        var invitation = await invitations.FindPendingByTokenHashAsync(tokenHash, ct);
        if (invitation is null)
            return Invalid;

        return new ValidateInvitationTokenResult(
            true,
            invitation.InviteeEmail,
            invitation.Organisation.Name,
            invitation.InviteeRole.ToString());
    }

    /// <summary>
    /// Attempts to decode <paramref name="rawToken"/> as a 64-character hex string
    /// and compute its SHA-256 hash. Returns <c>false</c> and leaves
    /// <paramref name="tokenHash"/> empty when the input is not a valid 32-byte
    /// hex-encoded token.
    /// </summary>
    internal static bool TryHashToken(string rawToken, out string tokenHash)
    {
        tokenHash = string.Empty;
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length != 64)
            return false;

        try
        {
            var bytes = Convert.FromHexString(rawToken);
            tokenHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
