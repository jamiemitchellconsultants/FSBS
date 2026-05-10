using System.Net.Http.Json;
using FSBS.Shared.InstructorSchedule;

namespace FSBS.Web.Services;

public sealed class InstructorScheduleService(HttpClient http)
{
    public async Task<InstructorScheduleDto> GetMyScheduleAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await GetScheduleAsync($"v1/instructors/me/schedule?from={Format(from)}&to={Format(to)}", ct);

    public async Task<InstructorScheduleDto> GetScheduleAsync(Guid instructorId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await GetScheduleAsync($"v1/instructors/{instructorId}/schedule?from={Format(from)}&to={Format(to)}", ct);

    public Task<WeeklyPatternDto> UpsertMyPatternAsync(WeeklyPatternUpsertRequest body, CancellationToken ct = default) =>
        PutAsync<WeeklyPatternDto>("v1/instructors/me/schedule/pattern", body, ct);

    public Task<WeeklyPatternDto> UpsertPatternAsync(Guid instructorId, WeeklyPatternUpsertRequest body, CancellationToken ct = default) =>
        PutAsync<WeeklyPatternDto>($"v1/instructors/{instructorId}/schedule/pattern", body, ct);

    public Task SetMySingleDayAsync(DateOnly date, SingleDayUpsertRequest body, CancellationToken ct = default) =>
        PutAsync($"v1/instructors/me/schedule/days/{Format(date)}", body, ct);

    public Task SetSingleDayAsync(Guid instructorId, DateOnly date, SingleDayUpsertRequest body, CancellationToken ct = default) =>
        PutAsync($"v1/instructors/{instructorId}/schedule/days/{Format(date)}", body, ct);

    public Task<AvailabilityOverrideDto> CreateMyOverrideAsync(OverrideRequest body, CancellationToken ct = default) =>
        PostAsync<AvailabilityOverrideDto>("v1/instructors/me/schedule/overrides", body, ct);

    public Task<AvailabilityOverrideDto> CreateOverrideAsync(Guid instructorId, OverrideRequest body, CancellationToken ct = default) =>
        PostAsync<AvailabilityOverrideDto>($"v1/instructors/{instructorId}/schedule/overrides", body, ct);

    public Task<AvailabilityOverrideDto> UpdateMyOverrideAsync(Guid overrideId, OverrideRequest body, CancellationToken ct = default) =>
        PutAsync<AvailabilityOverrideDto>($"v1/instructors/me/schedule/overrides/{overrideId}", body, ct);

    public Task<AvailabilityOverrideDto> UpdateOverrideAsync(Guid instructorId, Guid overrideId, OverrideRequest body, CancellationToken ct = default) =>
        PutAsync<AvailabilityOverrideDto>($"v1/instructors/{instructorId}/schedule/overrides/{overrideId}", body, ct);

    public async Task DeleteMyOverrideAsync(Guid overrideId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/instructors/me/schedule/overrides/{overrideId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteOverrideAsync(Guid instructorId, Guid overrideId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/instructors/{instructorId}/schedule/overrides/{overrideId}", ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<InstructorScheduleDto> GetScheduleAsync(string url, CancellationToken ct)
    {
        var result = await http.GetFromJsonAsync<InstructorScheduleDto>(url, ct);
        return result ?? throw new InvalidOperationException("Server returned an empty schedule payload.");
    }

    private async Task<T> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await http.PutAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private async Task PutAsync(string url, object body, CancellationToken ct)
    {
        var response = await http.PutAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private static string Format(DateOnly d) => d.ToString("yyyy-MM-dd");
}
