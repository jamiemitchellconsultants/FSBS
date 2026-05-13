using System.Net;
using System.Net.Http.Json;
using FSBS.Shared.Courses;

namespace FSBS.Web.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper for the Courses API. Currently
/// exposes only the create endpoint; list / detail methods will be filled in
/// when the read slice ships.
/// </summary>
public sealed class CourseService(HttpClient http)
{
    /// <summary>
    /// POST <c>/v1/courses</c>. Maps 409 Conflict to a thrown
    /// <see cref="InvalidOperationException"/> so callers can render a
    /// friendly message; other non-success codes throw via
    /// <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>.
    /// </summary>
    public async Task<CreateCourseResponse> CreateAsync(
        CreateCourseRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("v1/courses", request, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("A course with conflicting data already exists.");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateCourseResponse>(ct)
            ?? throw new InvalidOperationException("Unexpected empty response from server.");
    }
}
