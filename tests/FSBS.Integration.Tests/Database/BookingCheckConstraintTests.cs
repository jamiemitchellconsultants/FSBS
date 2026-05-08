using FSBS.Integration.Tests.Infrastructure;
using Npgsql;

namespace FSBS.Integration.Tests.Database;

/// <summary>
/// Asserts that the CHECK constraints on <c>bookings</c> and <c>booking_slots</c>
/// fire at the database level — these guarantee invariant enforcement even if
/// the application layer is bypassed (e.g. by a future direct-SQL admin tool).
/// </summary>
[Trait("Category", "Integration")]
public class BookingCheckConstraintTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    // ── booking_slots: minimum 240-minute duration ───────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(239)]   // one minute under the floor
    public async Task BookingSlot_DurationBelow240_RaisesCheckViolation(int durationMins)
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, _, bayId, _) = await DbSeeder.SeedBookingAsync(conn);

        var act = () => InsertBookingSlotAsync(conn, bookingId, bayId, durationMins);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_booking_slots_min_duration");
    }

    [Theory]
    [InlineData(240)]   // boundary — allowed
    [InlineData(241)]
    [InlineData(720)]
    public async Task BookingSlot_DurationAtOrAbove240_Succeeds(int durationMins)
    {
        await using var conn = await OpenConnectionAsync();
        var (bookingId, _, bayId, _) = await DbSeeder.SeedBookingAsync(conn);

        await InsertBookingSlotAsync(conn, bookingId, bayId, durationMins);
        // No exception means the row was accepted.
    }

    // ── bookings: FlightDeck capacity (≤4) ───────────────────────────────────

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task Booking_FlightDeckOverCap_RaisesCheckViolation(int studentCount)
    {
        await using var conn = await OpenConnectionAsync();

        var act = () => DbSeeder.SeedBookingAsync(conn, "FlightDeck", studentCount);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_bookings_fd_capacity");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]   // boundary
    public async Task Booking_FlightDeckAtOrUnderCap_Succeeds(int studentCount)
    {
        await using var conn = await OpenConnectionAsync();

        await DbSeeder.SeedBookingAsync(conn, "FlightDeck", studentCount);
    }

    // ── bookings: CabinCrew capacity (≤10) ───────────────────────────────────

    [Theory]
    [InlineData(11)]
    [InlineData(20)]
    public async Task Booking_CabinCrewOverCap_RaisesCheckViolation(int studentCount)
    {
        await using var conn = await OpenConnectionAsync();

        var act = () => DbSeeder.SeedBookingAsync(conn, "CabinCrew", studentCount);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_bookings_cc_capacity");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]   // boundary
    public async Task Booking_CabinCrewAtOrUnderCap_Succeeds(int studentCount)
    {
        await using var conn = await OpenConnectionAsync();

        await DbSeeder.SeedBookingAsync(conn, "CabinCrew", studentCount);
    }

    // ── bookings: student_count must be at least 1 ───────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Booking_StudentCountBelowOne_RaisesCheckViolation(int studentCount)
    {
        await using var conn = await OpenConnectionAsync();

        var act = () => DbSeeder.SeedBookingAsync(conn, "FlightDeck", studentCount);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_bookings_student_count");
    }

    private static async Task InsertBookingSlotAsync(
        NpgsqlConnection conn, Guid bookingId, Guid bayId, int durationMins)
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO booking_slots (booking_id, bay_id, start_at, end_at, duration_mins)
            VALUES (@bid, @bay, @s, @e, @d);
            """, conn);
        cmd.Parameters.AddWithValue("bid", bookingId);
        cmd.Parameters.AddWithValue("bay", bayId);
        cmd.Parameters.AddWithValue("s", start);
        cmd.Parameters.AddWithValue("e", start.AddMinutes(durationMins));
        cmd.Parameters.AddWithValue("d", durationMins);
        await cmd.ExecuteNonQueryAsync();
    }
}
