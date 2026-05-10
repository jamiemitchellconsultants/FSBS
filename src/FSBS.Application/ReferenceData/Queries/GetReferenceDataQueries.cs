using FSBS.Shared.ReferenceData;
using MediatR;

namespace FSBS.Application.ReferenceData.Queries;

public record GetCustomerClassesQuery : IRequest<IReadOnlyList<ReferenceItemDto>>;
public record GetDiscountTypesQuery   : IRequest<IReadOnlyList<ReferenceItemDto>>;
public record GetPaymentMethodsQuery  : IRequest<IReadOnlyList<ReferenceItemDto>>;
public record GetAccountStatusesQuery : IRequest<IReadOnlyList<AccountStatusDto>>;
