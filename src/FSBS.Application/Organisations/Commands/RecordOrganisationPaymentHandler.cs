using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.Organisations.Commands;

public sealed class RecordOrganisationPaymentHandler(
    IOrganisationAccountRepository accounts,
    ICurrentUser currentUser)
    : IRequestHandler<RecordOrganisationPaymentCommand, FSBS.Shared.Payments.PaymentDto>
{
    public Task<FSBS.Shared.Payments.PaymentDto> Handle(
        RecordOrganisationPaymentCommand request,
        CancellationToken ct) =>
        accounts.RecordPaymentAsync(
            request.OrgId,
            request.AmountGbp,
            request.PaymentDate,
            request.PaymentMethod,
            request.Reference,
            request.Notes,
            currentUser.UserId,
            ct);
}

