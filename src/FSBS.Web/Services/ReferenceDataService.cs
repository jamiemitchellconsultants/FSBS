using System.Net.Http.Json;
using FSBS.Shared.ReferenceData;

namespace FSBS.Web.Services;

public sealed class ReferenceDataService(HttpClient http)
{
    public Task<IReadOnlyList<ReferenceItemDto>> GetCustomerClassesAsync(CancellationToken ct = default) =>
        GetListAsync<ReferenceItemDto>("v1/reference-data/customer-classes", ct);

    public Task<IReadOnlyList<ReferenceItemDto>> GetDiscountTypesAsync(CancellationToken ct = default) =>
        GetListAsync<ReferenceItemDto>("v1/reference-data/discount-types", ct);

    public Task<IReadOnlyList<ReferenceItemDto>> GetPaymentMethodsAsync(CancellationToken ct = default) =>
        GetListAsync<ReferenceItemDto>("v1/reference-data/payment-methods", ct);

    public Task<IReadOnlyList<AccountStatusDto>> GetAccountStatusesAsync(CancellationToken ct = default) =>
        GetListAsync<AccountStatusDto>("v1/reference-data/account-statuses", ct);

    public async Task<ReferenceItemDto> UpsertCustomerClassAsync(UpsertReferenceItemRequest request, CancellationToken ct = default) =>
        await PutAsync<ReferenceItemDto>($"v1/reference-data/customer-classes/{request.Code}", request, ct);

    public async Task<ReferenceItemDto> UpsertDiscountTypeAsync(UpsertReferenceItemRequest request, CancellationToken ct = default) =>
        await PutAsync<ReferenceItemDto>($"v1/reference-data/discount-types/{request.Code}", request, ct);

    public async Task<ReferenceItemDto> UpsertPaymentMethodAsync(UpsertReferenceItemRequest request, CancellationToken ct = default) =>
        await PutAsync<ReferenceItemDto>($"v1/reference-data/payment-methods/{request.Code}", request, ct);

    public async Task<AccountStatusDto> UpsertAccountStatusAsync(UpsertAccountStatusRequest request, CancellationToken ct = default) =>
        await PutAsync<AccountStatusDto>($"v1/reference-data/account-statuses/{request.Code}", request, ct);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        var result = await http.GetFromJsonAsync<IReadOnlyList<T>>(url, ct);
        return result ?? [];
    }

    private async Task<T> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await http.PutAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}
