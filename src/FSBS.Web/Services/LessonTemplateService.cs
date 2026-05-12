using System.Net;
using System.Net.Http.Json;
using FSBS.Domain.Enums;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Web.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper for the lesson library API.
/// Maps 409 Conflict to a thrown <see cref="InvalidOperationException"/>
/// so callers can surface a friendly message; other non-success codes
/// throw via <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>.
/// </summary>
public sealed class LessonTemplateService(HttpClient http)
{
    /// <summary>Cursor-paginated library list.</summary>
    public async Task<PagedResult<LessonTemplateListItemDto>> ListAsync(
        TrainingType? trainingType = null,
        string? category = null,
        bool? isActive = null,
        string? search = null,
        string? cursor = null,
        int limit = 25,
        CancellationToken ct = default)
    {
        var qs = new List<string> { $"limit={limit}" };
        if (trainingType is { } tt) qs.Add($"trainingType={tt}");
        if (!string.IsNullOrWhiteSpace(category)) qs.Add($"category={Uri.EscapeDataString(category)}");
        if (isActive is { } a) qs.Add($"isActive={(a ? "true" : "false")}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(cursor)) qs.Add($"cursor={Uri.EscapeDataString(cursor)}");

        var url = "v1/lesson-templates?" + string.Join("&", qs);
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<LessonTemplateListItemDto>>(ct)
            ?? new PagedResult<LessonTemplateListItemDto>([], null);
    }

    /// <summary>Full detail by id. Returns null on 404.</summary>
    public async Task<LessonTemplateDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"v1/lesson-templates/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonTemplateDto>(ct);
    }

    /// <summary>Create a new template (writer roles only).</summary>
    public async Task<LessonTemplateDto> CreateAsync(CreateLessonTemplateRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/lesson-templates", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonTemplateDto>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }

    /// <summary>Update an existing template (writer roles only).</summary>
    public async Task<LessonTemplateDto> UpdateAsync(Guid id, UpdateLessonTemplateRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"v1/lesson-templates/{id}", request, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("This template was modified by someone else. Reload and try again.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonTemplateDto>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }

    /// <summary>Toggle the IsActive flag (writer roles only).</summary>
    public async Task<LessonTemplateDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"v1/lesson-templates/{id}/active",
            new SetLessonTemplateActiveRequest(isActive), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonTemplateDto>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }

    /// <summary>Soft-delete a template (writer roles only).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"v1/lesson-templates/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Copy a template into a new Lesson on the given module.</summary>
    public async Task<LessonDto> AttachToModuleAsync(
        Guid moduleId,
        AttachLessonToModuleRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            $"v1/modules/{moduleId}/lessons/from-template", request, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("That sequence number is already used in this module.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonDto>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }
}
