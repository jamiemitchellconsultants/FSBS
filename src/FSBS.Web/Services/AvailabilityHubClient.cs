using Fluxor;
using Microsoft.AspNetCore.SignalR.Client;
using FSBS.Web.State.Calendar;

namespace FSBS.Web.Services;

public sealed class AvailabilityHubClient(IDispatcher dispatcher, AuthService authService) : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly HashSet<Guid> _subscribedSimulatorIds = [];
    private bool _isStarted;

    public async Task StartAsync(string hubUrl, CancellationToken ct = default)
    {
        if (_isStarted && _connection is not null)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => authService.GetStoredTokenAsync(ct);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>("AvailabilityUpdated", delta =>
        {
            dispatcher.Dispatch(new ApplyCalendarDeltaAction(delta));
            dispatcher.Dispatch(new CalendarRealtimeRefreshRequestedAction(DateTimeOffset.UtcNow));
        });

        _connection.Reconnected += async _ =>
        {
            foreach (var simulatorId in _subscribedSimulatorIds)
            {
                await _connection.InvokeAsync("SubscribeToSimulator", simulatorId);
            }
        };

        await _connection.StartAsync(ct);
        _isStarted = true;
    }

    public async Task SetSubscribedSimulatorsAsync(IReadOnlyCollection<Guid> simulatorIds, CancellationToken ct = default)
    {
        if (_connection is null || !_isStarted)
        {
            return;
        }

        var wantedIds = simulatorIds.ToHashSet();

        foreach (var simulatorId in _subscribedSimulatorIds.Except(wantedIds).ToList())
        {
            await _connection.InvokeAsync("UnsubscribeFromSimulator", simulatorId, ct);
            _subscribedSimulatorIds.Remove(simulatorId);
        }

        foreach (var simulatorId in wantedIds.Except(_subscribedSimulatorIds).ToList())
        {
            await _connection.InvokeAsync("SubscribeToSimulator", simulatorId, ct);
            _subscribedSimulatorIds.Add(simulatorId);
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync(ct);
            _subscribedSimulatorIds.Clear();
            _isStarted = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
