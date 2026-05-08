using FSBS.Integration.Tests.Infrastructure;
using Npgsql;

namespace FSBS.Integration.Tests.Database;

/// <summary>
/// Asserts the booking_approvals CHECK constraints fire at the database level:
/// reviewer ≠ booker (self-approval ban) and rejection-reason length ≥ 10.
/// These mirror the application-layer rules so the rules survive even if a
/// caller bypasses the handler.
/// </summary>
[Trait("Category", "Integration")]
public class BookingApprovalCheckConstraintTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    // ── ck_booking_approvals_no_self_approval ────────────────────────────────

    [Fact]
    public async Task SelfApproval_RaisesCheckViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);

        // Same user as reviewed_by → check fails. Note decision must be non-Pending
        // for the reviewed-fields constraint to require reviewed_by, so we use Approved.
        var act = () => InsertApprovalAsync(
            conn, bookingId, requestedBy: bookerId, reviewedBy: bookerId,
            decision: "Approved");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_booking_approvals_no_self_approval");
    }

    [Fact]
    public async Task DifferentReviewer_Succeeds()
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);
        var reviewerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");

        await InsertApprovalAsync(
            conn, bookingId, requestedBy: bookerId, reviewedBy: reviewerId,
            decision: "Approved");
    }

    [Fact]
    public async Task PendingDecisionWithNullReviewer_Succeeds()
    {
        // NULL != bookerId is NULL in SQL — neither true nor false — so
        // the CHECK does not fail. This is the path taken when the approval
        // is first created in 'Pending' state.
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);

        await InsertApprovalAsync(
            conn, bookingId, requestedBy: bookerId, reviewedBy: null,
            decision: "Pending");
    }

    // ── ck_booking_approvals_rejection (reason length ≥ 10) ──────────────────

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nine char")]   // exactly 9 chars
    public async Task Rejection_WithReasonShorterThan10_RaisesCheckViolation(string reason)
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);
        var reviewerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");

        var act = () => InsertApprovalAsync(
            conn, bookingId,
            requestedBy: bookerId,
            reviewedBy: reviewerId,
            decision: "Rejected",
            rejectionReason: reason);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_booking_approvals_rejection");
    }

    [Fact]
    public async Task Rejection_WithoutReason_RaisesCheckViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);
        var reviewerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");

        var act = () => InsertApprovalAsync(
            conn, bookingId,
            requestedBy: bookerId,
            reviewedBy: reviewerId,
            decision: "Rejected",
            rejectionReason: null);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_booking_approvals_rejection");
    }

    [Theory]
    [InlineData("ten chars!")]   // exactly 10 — boundary
    [InlineData("Insufficient justification provided by booker")]
    public async Task Rejection_With10OrMoreCharReason_Succeeds(string reason)
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, bookerId, _, _) = await DbSeeder.SeedBookingAsync(conn);
        var reviewerId = await DbSeeder.SeedUserAsync(conn, role: "SalesStaff");

        await InsertApprovalAsync(
            conn, bookingId,
            requestedBy: bookerId,
            reviewedBy: reviewerId,
            decision: "Rejected",
            rejectionReason: reason);
    }

    private static async Task InsertApprovalAsync(
        NpgsqlConnection conn,
        Guid bookingId,
        Guid requestedBy,
        Guid? reviewedBy,
        string decision,
        string? rejectionReason = null)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO booking_approvals
                (approval_id, booking_id, requested_by, reviewed_by, reviewed_at,
                 decision, rejection_reason, department_name, budget_code)
            VALUES (@id, @bid, @rb, @rev, @revat,
                    @d::approval_decision, @reason, @dept, @bud);
            """, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("bid", bookingId);
        cmd.Parameters.AddWithValue("rb", requestedBy);
        cmd.Parameters.AddWithValue("rev", (object?)reviewedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("revat",
            reviewedBy is null ? DBNull.Value : DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("d", decision);
        cmd.Parameters.AddWithValue("reason", (object?)rejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dept", "Flight Ops");
        cmd.Parameters.AddWithValue("bud", "FO-2026-001");
        await cmd.ExecuteNonQueryAsync();
    }
}
