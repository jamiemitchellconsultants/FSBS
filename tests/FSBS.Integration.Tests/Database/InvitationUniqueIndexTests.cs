using FSBS.Integration.Tests.Infrastructure;
using Npgsql;

namespace FSBS.Integration.Tests.Database;

/// <summary>
/// Asserts the invitations uniqueness rules:
///   - <c>uq_invitations_token_hash</c>: token_hash unique across the table.
///   - <c>uq_invitations_pending_email_org</c>: partial unique on
///     <c>(invitee_email, org_id) WHERE status = 'Pending'</c>, allowing
///     re-invitation after Expired/Revoked/Claimed.
/// </summary>
[Trait("Category", "Integration")]
public class InvitationUniqueIndexTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task DuplicateTokenHash_RaisesUniqueViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var orgId = await DbSeeder.SeedOrganisationAsync(conn);
        var issuerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");
        var sharedHash = new string('a', 64);

        await InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: sharedHash, email: "first@example.test", status: "Pending");

        var act = () => InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: sharedHash, email: "second@example.test", status: "Pending");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.UniqueViolation);
        ex.Which.ConstraintName.Should().Be("uq_invitations_token_hash");
    }

    [Fact]
    public async Task DuplicatePendingForSameEmailAndOrg_RaisesUniqueViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var orgId = await DbSeeder.SeedOrganisationAsync(conn);
        var issuerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");
        const string email = "invitee@example.test";

        await InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: new string('a', 64), email: email, status: "Pending");

        var act = () => InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: new string('b', 64), email: email, status: "Pending");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.UniqueViolation);
        ex.Which.ConstraintName.Should().Be("uq_invitations_pending_email_org");
    }

    [Fact]
    public async Task ReinvitingAfterPriorExpired_Succeeds()
    {
        // Partial index excludes Expired rows, so a fresh Pending row for the
        // same (email, org) is allowed once the previous one has expired.
        await using var conn = await OpenConnectionAsync();
        var orgId = await DbSeeder.SeedOrganisationAsync(conn);
        var issuerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");
        const string email = "reinvited@example.test";

        await InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: new string('a', 64), email: email, status: "Expired");

        await InsertInvitationAsync(conn, orgId, issuerId,
            tokenHash: new string('b', 64), email: email, status: "Pending");
    }

    [Fact]
    public async Task DifferentOrg_AllowsSameEmailPending()
    {
        await using var conn = await OpenConnectionAsync();
        var orgA = await DbSeeder.SeedOrganisationAsync(conn);
        var orgB = await DbSeeder.SeedOrganisationAsync(conn);
        var issuerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");
        const string email = "shared@example.test";

        await InsertInvitationAsync(conn, orgA, issuerId,
            tokenHash: new string('a', 64), email: email, status: "Pending");
        await InsertInvitationAsync(conn, orgB, issuerId,
            tokenHash: new string('b', 64), email: email, status: "Pending");
    }

    private static async Task InsertInvitationAsync(
        NpgsqlConnection conn,
        Guid orgId,
        Guid issuedBy,
        string tokenHash,
        string email,
        string status)
    {
        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO invitations
                (invitation_id, token_hash, invitee_email, invitee_role,
                 org_id, issued_by, status)
            VALUES (@id, @hash, @email, 'CorporateStudent'::invitee_role,
                    @org, @by, @status::invitation_status);
            """, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("hash", tokenHash);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("by", issuedBy);
        cmd.Parameters.AddWithValue("status", status);
        await cmd.ExecuteNonQueryAsync();
    }
}
