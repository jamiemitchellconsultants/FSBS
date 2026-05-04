using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class SimulatorService(HttpClient http)
{
    public async Task<IReadOnlyList<SimulatorSummary>> GetSimulatorsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<SimulatorPage>("v1/simulators?limit=100", ct);
        return result?.Items ?? [];
    }

    public Task<object?> GetSimulatorAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<IReadOnlyList<object>> GetConfigurationsAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<IReadOnlyList<object>> GetMaintenanceWindowsAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<IReadOnlyList<object>> GetReconfigTemplatesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    private sealed record SimulatorPage(IReadOnlyList<SimulatorSummary> Items);
}

public sealed record SimulatorSummary(Guid UnitId, string Name, bool IsActive);

