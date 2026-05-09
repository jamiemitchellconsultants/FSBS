using System.Net.Http.Json;
using FSBS.Shared.Payments;

namespace FSBS.Web.Services;

public sealed class OrganisationService(HttpClient http)
{
    public async Task<IReadOnlyList<OrganisationSummaryDto>> GetOrganisationsAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<OrganisationListResponse>("v1/organisations", ct);
        return response?.Items ?? [];
    }

    public async Task<OrgAccountDto?> GetAccountAsync(Guid orgId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<OrgAccountDto>($"v1/organisations/{orgId}/account", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PaymentDto> RecordPaymentAsync(
        Guid orgId,
        RecordPaymentRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"v1/organisations/{orgId}/payments", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentDto>(ct))!;
    }

    private record OrganisationListResponse(IReadOnlyList<OrganisationSummaryDto> Items);
}

public record OrganisationSummaryDto(Guid Id, string Name);
