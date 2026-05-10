using FSBS.Application.Common.Interfaces;
using FSBS.Shared.ReferenceData;
using MediatR;

namespace FSBS.Application.ReferenceData.Queries;

public sealed class GetCustomerClassesHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetCustomerClassesQuery, IReadOnlyList<ReferenceItemDto>>
{
    public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetCustomerClassesQuery _, CancellationToken ct) =>
        repo.GetCustomerClassesAsync(ct);
}

public sealed class GetDiscountTypesHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetDiscountTypesQuery, IReadOnlyList<ReferenceItemDto>>
{
    public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetDiscountTypesQuery _, CancellationToken ct) =>
        repo.GetDiscountTypesAsync(ct);
}

public sealed class GetPaymentMethodsHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetPaymentMethodsQuery, IReadOnlyList<ReferenceItemDto>>
{
    public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetPaymentMethodsQuery _, CancellationToken ct) =>
        repo.GetPaymentMethodsAsync(ct);
}

public sealed class GetAccountStatusesHandler(IReferenceDataRepository repo)
    : IRequestHandler<GetAccountStatusesQuery, IReadOnlyList<AccountStatusDto>>
{
    public Task<IReadOnlyList<AccountStatusDto>> Handle(GetAccountStatusesQuery _, CancellationToken ct) =>
        repo.GetAccountStatusesAsync(ct);
}
