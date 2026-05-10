using FSBS.Shared.ReferenceData;
using MediatR;

namespace FSBS.Application.ReferenceData.Commands;

public record UpsertCustomerClassCommand(UpsertReferenceItemRequest Item)    : IRequest<ReferenceItemDto>;
public record UpsertDiscountTypeCommand(UpsertReferenceItemRequest Item)     : IRequest<ReferenceItemDto>;
public record UpsertPaymentMethodCommand(UpsertReferenceItemRequest Item)    : IRequest<ReferenceItemDto>;
public record UpsertAccountStatusCommand(UpsertAccountStatusRequest Item)    : IRequest<AccountStatusDto>;
