using FSBS.Shared.ReferenceData;

namespace FSBS.Application.Common.Interfaces;

public interface IReferenceDataRepository
{
    Task<IReadOnlyList<ReferenceItemDto>> GetCustomerClassesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceItemDto>> GetDiscountTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceItemDto>> GetPaymentMethodsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AccountStatusDto>> GetAccountStatusesAsync(CancellationToken ct = default);

    Task<ReferenceItemDto> UpsertCustomerClassAsync(UpsertReferenceItemRequest request, CancellationToken ct = default);
    Task<ReferenceItemDto> UpsertDiscountTypeAsync(UpsertReferenceItemRequest request, CancellationToken ct = default);
    Task<ReferenceItemDto> UpsertPaymentMethodAsync(UpsertReferenceItemRequest request, CancellationToken ct = default);
    Task<AccountStatusDto> UpsertAccountStatusAsync(UpsertAccountStatusRequest request, CancellationToken ct = default);
}
