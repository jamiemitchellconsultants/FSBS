using FSBS.Application.Common.Interfaces;
using FSBS.Shared.ReferenceData;

namespace FSBS.Application.ReferenceData.Commands;

public record UpsertCustomerClassCommand(UpsertReferenceItemRequest Item)  : ICommand<ReferenceItemDto>;
public record UpsertDiscountTypeCommand(UpsertReferenceItemRequest Item)   : ICommand<ReferenceItemDto>;
public record UpsertPaymentMethodCommand(UpsertReferenceItemRequest Item)  : ICommand<ReferenceItemDto>;
public record UpsertAccountStatusCommand(UpsertAccountStatusRequest Item)  : ICommand<AccountStatusDto>;
