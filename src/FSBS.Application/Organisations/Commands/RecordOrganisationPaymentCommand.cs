using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Payments;

namespace FSBS.Application.Organisations.Commands;

public record RecordOrganisationPaymentCommand(
    Guid OrgId,
    decimal AmountGbp,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? Reference,
    string? Notes) : ICommand<PaymentDto>;


