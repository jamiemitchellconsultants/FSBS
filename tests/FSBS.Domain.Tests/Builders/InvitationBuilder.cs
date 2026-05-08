using System.Security.Cryptography;
using System.Text;

namespace FSBS.Domain.Tests.Builders;

/// <summary>
/// Builds <see cref="Invitation"/> instances. The token-hash helper produces the
/// SHA-256 hex digest the production code stores so that round-trip tests can
/// validate the hashing contract without leaking raw tokens into fixtures.
/// </summary>
public sealed class InvitationBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _orgId = Guid.NewGuid();
    private string _email = "invitee@example.com";
    private InviteeRole _role = InviteeRole.CorporateStudent;
    private string _tokenHash = HashToken(Guid.NewGuid().ToString("N"));
    private InvitationStatus _status = InvitationStatus.Pending;
    private DateTimeOffset _expiresAt = DateTimeOffset.UtcNow.AddDays(7);

    public InvitationBuilder WithId(Guid id) { _id = id; return this; }
    public InvitationBuilder WithOrg(Guid orgId) { _orgId = orgId; return this; }
    public InvitationBuilder WithEmail(string email) { _email = email; return this; }
    public InvitationBuilder WithRole(InviteeRole role) { _role = role; return this; }
    public InvitationBuilder WithStatus(InvitationStatus status) { _status = status; return this; }
    public InvitationBuilder WithExpiry(DateTimeOffset expiresAt) { _expiresAt = expiresAt; return this; }

    public InvitationBuilder WithRawToken(string rawToken) { _tokenHash = HashToken(rawToken); return this; }
    public InvitationBuilder WithTokenHash(string hash) { _tokenHash = hash; return this; }

    public Invitation Build() => new()
    {
        Id = _id,
        OrgId = _orgId,
        InviteeEmail = _email,
        InviteeRole = _role,
        TokenHash = _tokenHash,
        Status = _status,
        ExpiresAt = _expiresAt,
    };

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
