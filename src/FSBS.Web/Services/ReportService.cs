namespace FSBS.Web.Services;

public sealed class ReportService(HttpClient http)
{
    public Task<IReadOnlyList<object>> GetReportsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<object?> GetReportAsync(Guid reportId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<Guid> RunReportAsync(Guid reportId, CancellationToken ct = default) =>
        Task.FromResult(Guid.Empty);

    public Task<object?> GetRunAsync(Guid runId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);
}
