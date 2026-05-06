namespace FSBS.Web.Services;

public sealed class CourseService(HttpClient http)
{
    private readonly HttpClient _http = http;
    public Task<IReadOnlyList<object>> GetCoursesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<object?> GetCourseAsync(Guid courseId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<IReadOnlyList<object>> GetEnrolmentsAsync(string? afterCursor = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<object?> GetEnrolmentAsync(Guid enrolmentId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task SignOffProgressAsync(Guid enrolmentId, Guid lessonId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
