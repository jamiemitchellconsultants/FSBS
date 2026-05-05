using System.Data;
using Dapper;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper-based implementation of <see cref="IAvailabilityReadService"/>.
/// Executes a single SQL query that returns available slots, reconfiguration
/// windows, and maintenance windows in one round-trip, bypassing EF Core for
/// performance on the hot availability-grid path.
/// </summary>
internal sealed class AvailabilityReadService(IDbConnection db) : IAvailabilityReadService
{
    public async Task<AvailabilityGridDto> GetAvailabilityAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        const string sql = """
            -- Available slots: bays with remaining capacity in the window
            SELECT
                bs.bay_id                           AS BayId,
                bs.start_at                         AS StartAt,
                bs.end_at                           AS EndAt,
                sc.max_capacity_flight_deck
                    - COUNT(bk.id)                  AS RemainingCapacity,
                'available'                         AS RowKind
            FROM fsbs.simulator_bays sb
            JOIN fsbs.simulator_units su ON su.id = sb.simulator_unit_id
            JOIN fsbs.simulator_configurations sc ON sc.id = su.active_configuration_id
            JOIN fsbs.booking_slots bs ON bs.bay_id = sb.id
            LEFT JOIN fsbs.bookings bk
                ON bk.id = bs.booking_id
               AND bk.status NOT IN ('CancelledByCustomer','CancelledByAdmin','Rejected','Expired')
            WHERE su.id = @SimulatorId
              AND bs.start_at >= @From
              AND bs.end_at   <= @To
              AND bs.slot_status NOT IN ('Cancelled')
              AND sb.is_deleted = false
              AND su.is_deleted = false
            GROUP BY bs.bay_id, bs.start_at, bs.end_at,
                     sc.max_capacity_flight_deck

            UNION ALL

            -- Reconfiguration windows
            SELECT
                rs.bay_id                           AS BayId,
                rs.start_at                         AS StartAt,
                rs.end_at                           AS EndAt,
                0                                   AS RemainingCapacity,
                'reconfig'                          AS RowKind
            FROM fsbs.reconfiguration_slots rs
            JOIN fsbs.simulator_bays sb ON sb.id = rs.bay_id
            JOIN fsbs.simulator_units su ON su.id = sb.simulator_unit_id
            WHERE su.id = @SimulatorId
              AND rs.start_at >= @From
              AND rs.end_at   <= @To

            UNION ALL

            -- Maintenance windows
            SELECT
                mw.bay_id                           AS BayId,
                mw.start_at                         AS StartAt,
                mw.end_at                           AS EndAt,
                0                                   AS RemainingCapacity,
                'maintenance'                       AS RowKind
            FROM fsbs.maintenance_windows mw
            JOIN fsbs.simulator_bays sb ON sb.id = mw.bay_id
            JOIN fsbs.simulator_units su ON su.id = sb.simulator_unit_id
            WHERE su.id = @SimulatorId
              AND mw.start_at >= @From
              AND mw.end_at   <= @To

            ORDER BY StartAt;
            """;

        var rows = await db.QueryAsync<RawAvailabilityRow>(
            new CommandDefinition(sql, new { SimulatorId = simulatorId, From = from, To = to },
                cancellationToken: ct));

        var availableSlots       = new List<AvailableSlotDto>();
        var reconfigWindows      = new List<ReconfigurationWindowDto>();
        var maintenanceWindows   = new List<MaintenanceWindowDto>();

        foreach (var row in rows)
        {
            switch (row.RowKind)
            {
                case "available":
                    availableSlots.Add(new AvailableSlotDto(
                        row.BayId, row.StartAt, row.EndAt, row.RemainingCapacity));
                    break;
                case "reconfig":
                    reconfigWindows.Add(new ReconfigurationWindowDto(
                        row.BayId, row.StartAt, row.EndAt,
                        string.Empty, string.Empty,
                        (int)(row.EndAt - row.StartAt).TotalMinutes));
                    break;
                case "maintenance":
                    maintenanceWindows.Add(new MaintenanceWindowDto(
                        row.BayId, row.StartAt, row.EndAt, null));
                    break;
            }
        }

        return new AvailabilityGridDto(
            simulatorId, from, to,
            availableSlots,
            reconfigWindows,
            maintenanceWindows);
    }

    private sealed record RawAvailabilityRow(
        Guid BayId,
        DateTimeOffset StartAt,
        DateTimeOffset EndAt,
        int RemainingCapacity,
        string RowKind);
}
