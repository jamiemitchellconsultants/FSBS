using System.Data.Common;
using FSBS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSBS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="DbConnectionInterceptor"/> that sets the PostgreSQL
/// session-level variable <c>app.current_tenant_id</c> immediately after each
/// connection is opened, so that the row-level security (RLS) policies on
/// tenant-scoped tables (<c>bookings</c>, <c>enrolments</c>, <c>courses</c>,
/// <c>organisations</c>, <c>invitations</c>, <c>invoices</c>) are enforced.
/// </summary>
/// <remarks>
/// Running <c>SET app.current_tenant_id</c> on connection open means the value
/// is in place before any query executes. Staff users always carry the school's
/// root tenant GUID. When <see cref="ICurrentUser.TenantId"/> is
/// <see cref="Guid.Empty"/> (unauthenticated requests) the variable is set to a
/// zero UUID which will not match any tenant row — safe-by-default behaviour.
/// </remarks>
public sealed class TenantCommandInterceptor(ICurrentUser currentUser) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantId(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantIdAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetTenantId(DbConnection connection)
    {
        // amazonq-ignore-next-line
        using var cmd = connection.CreateCommand();
        // amazonq-ignore-next-line
        cmd.CommandText = $"SET app.current_tenant_id = '{currentUser.TenantId}'";
        cmd.ExecuteNonQuery();
    }

    private async Task SetTenantIdAsync(DbConnection connection, CancellationToken ct)
    {
        // amazonq-ignore-next-line
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.current_tenant_id = '{currentUser.TenantId}'";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
