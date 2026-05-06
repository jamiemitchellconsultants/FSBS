using FSBS.Application.Simulators.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FSBS.Api.Hubs;

/// <summary>
/// SignalR hub that pushes real-time availability delta messages to all
/// connected clients whenever a booking is created, modified, or cancelled.
/// </summary>
/// <remarks>
/// <para>
/// Clients subscribe to a simulator's availability stream by calling
/// <c>SubscribeToSimulator(simulatorId)</c> after connecting. They are placed
/// in a SignalR group named <c>simulator:{simulatorId}</c> and receive
/// <c>AvailabilityUpdated</c> messages whenever that simulator's grid changes.
/// </para>
/// <para>
/// The Redis (ElastiCache) backplane is configured in <c>Program.cs</c> so
/// that messages are fanned out across all Fargate task instances.
/// </para>
/// <para>
/// The hub requires an authenticated user — the default authorization policy
/// (Staff or Customer JWT) applies.
/// </para>
/// </remarks>
[Authorize]
public sealed class AvailabilityHub : Hub
{
    private const string GroupPrefix = "simulator:";

    /// <summary>
    /// Called by the client to start receiving availability updates for a
    /// specific simulator. The client is added to the group
    /// <c>simulator:{simulatorId}</c>.
    /// </summary>
    public async Task SubscribeToSimulator(Guid simulatorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(simulatorId));
    }

    /// <summary>
    /// Called by the client to stop receiving updates for a simulator.
    /// </summary>
    public async Task UnsubscribeFromSimulator(Guid simulatorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(simulatorId));
    }

    // ── Static helper used by server-side code to push deltas ────────────────

    /// <summary>
    /// Pushes an updated availability grid to all clients subscribed to the
    /// given simulator. Called from booking command handlers (via
    /// <see cref="IAvailabilityHubContext"/>) after every booking mutation.
    /// </summary>
    public static Task SendAvailabilityUpdatedAsync(
        IHubContext<AvailabilityHub> hubContext,
        Guid simulatorId,
        AvailabilityGridDto grid,
        CancellationToken ct = default)
    {
        return hubContext.Clients
            .Group(GroupName(simulatorId))
            .SendAsync("AvailabilityUpdated", grid, ct);
    }

    private static string GroupName(Guid simulatorId) => $"{GroupPrefix}{simulatorId}";
}
