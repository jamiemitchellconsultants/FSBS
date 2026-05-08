using FSBS.Integration.Tests.Infrastructure;
using Npgsql;

namespace FSBS.Integration.Tests.Database;

/// <summary>
/// Asserts the bookings idempotency uniqueness and the booking_slots bay-level
/// overlap unique index. The schema now enforces bay-level overlap at the DB
/// (it formerly delegated this to the application layer).
/// </summary>
[Trait("Category", "Integration")]
public class BookingUniqueIndexTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    // ── uq_bookings_idempotency_key ──────────────────────────────────────────

    [Fact]
    public async Task DuplicateIdempotencyKey_RaisesUniqueViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var sharedKey = Guid.NewGuid();

        await InsertBookingWithIdempotencyAsync(conn, sharedKey);

        var act = () => InsertBookingWithIdempotencyAsync(conn, sharedKey);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.UniqueViolation);
        ex.Which.ConstraintName.Should().Be("uq_bookings_idempotency_key");
    }

    // ── uq_booking_slots_bay_time (bay-level no-overlap) ─────────────────────

    [Fact]
    public async Task TwoActiveSlotsSameBayAndTimeWindow_RaisesUniqueViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var (firstBooking,  _, bayId, _) = await DbSeeder.SeedBookingAsync(conn);
        var (secondBooking, _, _,     _) = await DbSeeder.SeedBookingAsync(conn);
        var start = DateTimeOffset.UtcNow.AddDays(1);

        await InsertSlotAsync(conn, firstBooking, bayId, start, 240, "Scheduled");

        var act = () => InsertSlotAsync(conn, secondBooking, bayId, start, 240, "Scheduled");

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.UniqueViolation);
        ex.Which.ConstraintName.Should().Be("uq_booking_slots_bay_time");
    }

    [Fact]
    public async Task CancelledSlotAtSameBayAndTime_DoesNotBlockNewActiveSlot()
    {
        // The partial index excludes Cancelled rows, so a fresh active slot at
        // the same (bay, start, end) is allowed once the prior is cancelled.
        await using var conn = await OpenConnectionAsync();
        var (firstBooking,  _, bayId, _) = await DbSeeder.SeedBookingAsync(conn);
        var (secondBooking, _, _,     _) = await DbSeeder.SeedBookingAsync(conn);
        var start = DateTimeOffset.UtcNow.AddDays(1);

        await InsertSlotAsync(conn, firstBooking,  bayId, start, 240, "Cancelled");
        await InsertSlotAsync(conn, secondBooking, bayId, start, 240, "Scheduled");
    }

    [Fact]
    public async Task DifferentBays_SameTimeWindow_AreAllowed()
    {
        // The unique index keys on bay_id, so the same time window on different
        // bays is fine — multiple simulator bays can run sessions concurrently.
        await using var conn = await OpenConnectionAsync();
        var (firstBooking,  _, firstBay,  _) = await DbSeeder.SeedBookingAsync(conn);
        var (secondBooking, _, secondBay, _) = await DbSeeder.SeedBookingAsync(conn);
        var start = DateTimeOffset.UtcNow.AddDays(1);

        await InsertSlotAsync(conn, firstBooking,  firstBay,  start, 240, "Scheduled");
        await InsertSlotAsync(conn, secondBooking, secondBay, start, 240, "Scheduled");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task InsertBookingWithIdempotencyAsync(
        NpgsqlConnection conn, Guid idempotencyKey)
    {
        var bookerId = await DbSeeder.SeedUserAsync(conn);
        var configId = await DbSeeder.SeedSimulatorConfigurationAsync(conn);
        var unitId = await DbSeeder.SeedSimulatorUnitAsync(conn);
        var bayId = await DbSeeder.SeedSimulatorBayAsync(conn, unitId);

        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO bookings
                (booking_id, bay_id, config_id, training_type, student_count,
                 booked_by, idempotency_key)
            VALUES (@id, @bay, @cfg, 'FlightDeck'::training_type, 1, @by, @idem);
            """, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("bay", bayId);
        cmd.Parameters.AddWithValue("cfg", configId);
        cmd.Parameters.AddWithValue("by", bookerId);
        cmd.Parameters.AddWithValue("idem", idempotencyKey);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertSlotAsync(
        NpgsqlConnection conn,
        Guid bookingId,
        Guid bayId,
        DateTimeOffset start,
        int durationMins,
        string slotStatus)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO booking_slots
                (slot_id, booking_id, bay_id, start_at, end_at, duration_mins, slot_status)
            VALUES (@id, @bid, @bay, @s, @e, @d, @ss::slot_status);
            """, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("bid", bookingId);
        cmd.Parameters.AddWithValue("bay", bayId);
        cmd.Parameters.AddWithValue("s", start);
        cmd.Parameters.AddWithValue("e", start.AddMinutes(durationMins));
        cmd.Parameters.AddWithValue("d", durationMins);
        cmd.Parameters.AddWithValue("ss", slotStatus);
        await cmd.ExecuteNonQueryAsync();
    }
}
