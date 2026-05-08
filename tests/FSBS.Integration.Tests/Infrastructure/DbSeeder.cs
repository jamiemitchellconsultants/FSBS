using Npgsql;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Minimal-graph seeder for DB constraint tests. Each method inserts the
/// fewest rows needed to satisfy foreign keys for a target row, returning
/// the new ID. Tests then perform the focused insert that should violate the
/// constraint under test.
/// </summary>
/// <remarks>
/// Connect with the container's superuser (<c>postgres</c>) so RLS is bypassed.
/// All inserts run on the <c>fsbs</c> schema via the shared search_path.
/// </remarks>
internal static class DbSeeder
{
    public static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SET search_path = fsbs, public;", conn);
        await cmd.ExecuteNonQueryAsync();
        return conn;
    }

    public static async Task<Guid> SeedUserAsync(
        NpgsqlConnection conn,
        string? email = null,
        string role = "InternalStudent",
        Guid? tenantId = null)
    {
        var userId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO users (user_id, cognito_sub, email, role, tenant_id)
            VALUES (@id, @sub, @email, @role::app_role, @tenant);
            """, conn);
        cmd.Parameters.AddWithValue("id", userId);
        cmd.Parameters.AddWithValue("sub", $"sub-{userId:N}");
        cmd.Parameters.AddWithValue("email", email ?? $"{userId:N}@example.test");
        cmd.Parameters.AddWithValue("role", role);
        cmd.Parameters.AddWithValue("tenant", tenantId ?? Guid.NewGuid());
        await cmd.ExecuteNonQueryAsync();
        return userId;
    }

    public static async Task<Guid> SeedOrganisationAsync(
        NpgsqlConnection conn,
        Guid? tenantId = null)
    {
        var orgId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO organisations (org_id, name, tenant_id, billing_email)
            VALUES (@id, @name, @tenant, @email);
            """, conn);
        cmd.Parameters.AddWithValue("id", orgId);
        cmd.Parameters.AddWithValue("name", $"Org-{orgId:N}");
        cmd.Parameters.AddWithValue("tenant", tenantId ?? Guid.NewGuid());
        cmd.Parameters.AddWithValue("email", $"billing-{orgId:N}@example.test");
        await cmd.ExecuteNonQueryAsync();
        return orgId;
    }

    public static async Task<Guid> SeedSimulatorConfigurationAsync(
        NpgsqlConnection conn,
        string aircraftType = "B737",
        string trainingType = "FlightDeck")
    {
        var configId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO simulator_configurations
                (config_id, name, aircraft_type, config_mode, supported_training_types)
            VALUES (@id, @name, @ac, 'CockpitOnly'::configuration_mode,
                    ARRAY[@tt::training_type]);
            """, conn);
        cmd.Parameters.AddWithValue("id", configId);
        cmd.Parameters.AddWithValue("name", $"Config-{configId:N}");
        cmd.Parameters.AddWithValue("ac", aircraftType);
        cmd.Parameters.AddWithValue("tt", trainingType);
        await cmd.ExecuteNonQueryAsync();
        return configId;
    }

    public static async Task<Guid> SeedSimulatorUnitAsync(NpgsqlConnection conn)
    {
        var unitId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO simulator_units (unit_id, name, fstd_level)
            VALUES (@id, @name, 'D');
            """, conn);
        cmd.Parameters.AddWithValue("id", unitId);
        cmd.Parameters.AddWithValue("name", $"Unit-{unitId:N}");
        await cmd.ExecuteNonQueryAsync();
        return unitId;
    }

    public static async Task<Guid> SeedSimulatorBayAsync(NpgsqlConnection conn, Guid unitId)
    {
        var bayId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO simulator_bays (bay_id, unit_id, bay_code)
            VALUES (@id, @unit, @code);
            """, conn);
        cmd.Parameters.AddWithValue("id", bayId);
        cmd.Parameters.AddWithValue("unit", unitId);
        cmd.Parameters.AddWithValue("code", $"B-{bayId:N}".Substring(0, 18));
        await cmd.ExecuteNonQueryAsync();
        return bayId;
    }

    /// <summary>
    /// Seeds users, sim, bay, config, then inserts a booking with the supplied
    /// training_type and student_count. Returns the booking_id and the booker's
    /// user_id so callers can use it for booking_approvals.requested_by.
    /// </summary>
    public static async Task<(Guid BookingId, Guid BookerId, Guid BayId, Guid ConfigId)>
        SeedBookingAsync(
            NpgsqlConnection conn,
            string trainingType = "FlightDeck",
            int studentCount = 1)
    {
        var bookerId = await SeedUserAsync(conn);
        var configId = await SeedSimulatorConfigurationAsync(conn, trainingType: trainingType);
        var unitId = await SeedSimulatorUnitAsync(conn);
        var bayId = await SeedSimulatorBayAsync(conn, unitId);

        var bookingId = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO bookings
                (booking_id, bay_id, config_id, training_type, student_count,
                 booked_by, idempotency_key)
            VALUES (@id, @bay, @cfg, @tt::training_type, @sc, @by, @idem);
            """, conn);
        cmd.Parameters.AddWithValue("id", bookingId);
        cmd.Parameters.AddWithValue("bay", bayId);
        cmd.Parameters.AddWithValue("cfg", configId);
        cmd.Parameters.AddWithValue("tt", trainingType);
        cmd.Parameters.AddWithValue("sc", studentCount);
        cmd.Parameters.AddWithValue("by", bookerId);
        cmd.Parameters.AddWithValue("idem", Guid.NewGuid());
        await cmd.ExecuteNonQueryAsync();
        return (bookingId, bookerId, bayId, configId);
    }
}
