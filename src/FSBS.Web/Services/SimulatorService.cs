using System.Net.Http.Json;
using FSBS.Shared.Simulators;

namespace FSBS.Web.Services;

public sealed class SimulatorService(HttpClient http)
{
    public async Task<IReadOnlyList<SimulatorSummary>> GetSimulatorsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<SimulatorListResponse>("v1/simulators", ct);
        return result?.Items
            .Select(d => new SimulatorSummary(d.UnitId, d.Name, d.IsActive))
            .ToList() ?? [];
    }

    public async Task<SimulatorDetailDto?> GetSimulatorDetailAsync(Guid simulatorId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<SimulatorDetailDto>($"v1/simulators/{simulatorId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private sealed record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);
}

public sealed record SimulatorSummary(Guid UnitId, string Name, bool IsActive);
