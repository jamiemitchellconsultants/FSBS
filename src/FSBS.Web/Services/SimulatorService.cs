namespace FSBS.Web.Services;

public sealed class SimulatorService(HttpClient http)
{
    public Task<IReadOnlyList<object>> GetSimulatorsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<object?> GetSimulatorAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<IReadOnlyList<object>> GetConfigurationsAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<IReadOnlyList<object>> GetMaintenanceWindowsAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<IReadOnlyList<object>> GetReconfigTemplatesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);
}
