using FSBS.Shared.Payments;
using MediatR;

namespace FSBS.Application.Organisations.Commands;

public record RecordOrganisationPaymentCommand(
    Guid OrgId,
    decimal AmountGbp,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? Reference,
    string? Notes) : IRequest<PaymentDto>;


