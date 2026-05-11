using System.Net.Http.Json;
using FSBS.Shared.AircraftTypes;
using FSBS.Shared.Simulators;

namespace FSBS.Web.Services;

public sealed class AircraftTypeService(HttpClient http)
{
    public async Task<IReadOnlyList<AircraftTypeDto>> GetAircraftTypesAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<IReadOnlyList<AircraftTypeDto>>("v1/aircraft-types", ct);
        return result ?? [];
    }

    public async Task<AircraftTypeDto> CreateAsync(string icaoCode, string name, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "v1/aircraft-types",
            new CreateAircraftTypeRequest(icaoCode, name),
            ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AircraftTypeDto>(ct))!;
    }

    public async Task<AircraftTypeDto> UpdateAsync(Guid id, string icaoCode, string name, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"v1/aircraft-types/{id}",
            new UpdateAircraftTypeRequest(icaoCode, name, isActive),
            ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AircraftTypeDto>(ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/aircraft-types/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
