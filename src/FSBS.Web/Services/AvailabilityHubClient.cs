using Fluxor;
using Microsoft.AspNetCore.SignalR.Client;
using FSBS.Web.State.Calendar;

namespace FSBS.Web.Services;

public sealed class AvailabilityHubClient(IDispatcher dispatcher) : IAsyncDisposable
{
    private HubConnection? _connection;

    public async Task StartAsync(string hubUrl, CancellationToken ct = default)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>("SlotUpdated", delta =>
            dispatcher.Dispatch(new ApplyCalendarDeltaAction(delta)));

        await _connection.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
            await _connection.StopAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
