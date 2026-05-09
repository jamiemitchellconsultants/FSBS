using System.Net.Http.Json;
using FSBS.Shared.Simulators;

namespace FSBS.Web.Services;

public sealed class SimulatorService(HttpClient http)
{
    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SimulatorSummary>> GetSimulatorsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<SimulatorListResponse>("v1/simulators", ct);
        return result?.Items
            .Select(d => new SimulatorSummary(d.UnitId, d.Name, d.IsActive))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<SimulatorDetailDto>> GetSimulatorDetailsAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<SimulatorListResponse>("v1/simulators", ct);
        return result?.Items ?? [];
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

    // ── SimulatorUnit write ───────────────────────────────────────────────────

    public async Task<SimulatorDetailDto> CreateUnitAsync(CreateSimulatorUnitRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/simulators", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorDetailDto>(ct))!;
    }

    public async Task<SimulatorDetailDto> UpdateUnitAsync(Guid id, UpdateSimulatorUnitRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"v1/simulators/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorDetailDto>(ct))!;
    }

    public async Task DeleteUnitAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/simulators/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── SimulatorBay write ────────────────────────────────────────────────────

    public async Task<SimulatorBayDto> CreateBayAsync(Guid unitId, CreateSimulatorBayRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"v1/simulators/{unitId}/bays", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorBayDto>(ct))!;
    }

    public async Task<SimulatorBayDto> UpdateBayAsync(Guid unitId, Guid bayId, UpdateSimulatorBayRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"v1/simulators/{unitId}/bays/{bayId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorBayDto>(ct))!;
    }

    public async Task DeleteBayAsync(Guid unitId, Guid bayId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/simulators/{unitId}/bays/{bayId}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── SimulatorConfiguration write ──────────────────────────────────────────

    public async Task<SimulatorConfigurationDto> CreateConfigurationAsync(Guid unitId, CreateSimulatorConfigurationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"v1/simulators/{unitId}/configurations", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorConfigurationDto>(ct))!;
    }

    public async Task<SimulatorConfigurationDto> UpdateConfigurationAsync(Guid unitId, Guid configId, UpdateSimulatorConfigurationRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"v1/simulators/{unitId}/configurations/{configId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulatorConfigurationDto>(ct))!;
    }

    public async Task DeleteConfigurationAsync(Guid unitId, Guid configId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/simulators/{unitId}/configurations/{configId}", ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);
}

public sealed record SimulatorSummary(Guid UnitId, string Name, bool IsActive);
