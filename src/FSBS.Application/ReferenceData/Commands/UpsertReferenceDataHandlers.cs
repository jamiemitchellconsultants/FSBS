using FSBS.Application.Common.Interfaces;
using FSBS.Shared.ReferenceData;
using MediatR;

namespace FSBS.Application.ReferenceData.Commands;

public sealed class UpsertCustomerClassHandler(IReferenceDataRepository repo)
    : IRequestHandler<UpsertCustomerClassCommand, ReferenceItemDto>
{
    public Task<ReferenceItemDto> Handle(UpsertCustomerClassCommand request, CancellationToken ct) =>
        repo.UpsertCustomerClassAsync(request.Item, ct);
}

public sealed class UpsertDiscountTypeHandler(IReferenceDataRepository repo)
    : IRequestHandler<UpsertDiscountTypeCommand, ReferenceItemDto>
{
    public Task<ReferenceItemDto> Handle(UpsertDiscountTypeCommand request, CancellationToken ct) =>
        repo.UpsertDiscountTypeAsync(request.Item, ct);
}

public sealed class UpsertPaymentMethodHandler(IReferenceDataRepository repo)
    : IRequestHandler<UpsertPaymentMethodCommand, ReferenceItemDto>
{
    public Task<ReferenceItemDto> Handle(UpsertPaymentMethodCommand request, CancellationToken ct) =>
        repo.UpsertPaymentMethodAsync(request.Item, ct);
}

public sealed class UpsertAccountStatusHandler(IReferenceDataRepository repo)
    : IRequestHandler<UpsertAccountStatusCommand, AccountStatusDto>
{
    public Task<AccountStatusDto> Handle(UpsertAccountStatusCommand request, CancellationToken ct) =>
        repo.UpsertAccountStatusAsync(request.Item, ct);
}
