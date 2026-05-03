using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class OrganisationService(HttpClient http)
{
    public async Task<IReadOnlyList<OrganisationSummaryDto>> GetOrganisationsAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<OrganisationListResponse>("v1/organisations", ct);
        return response?.Items ?? [];
    }

    public Task<object?> GetOrganisationAsync(Guid orgId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<object?> GetAccountAsync(Guid orgId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task RecordPaymentAsync(Guid orgId, decimal amountGbp, string method, string reference, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task VerifyPaymentAsync(Guid orgId, Guid paymentId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task VoidPaymentAsync(Guid orgId, Guid paymentId, string reason, CancellationToken ct = default) =>
        Task.CompletedTask;

    private record OrganisationListResponse(IReadOnlyList<OrganisationSummaryDto> Items);
}

public record OrganisationSummaryDto(Guid Id, string Name);
