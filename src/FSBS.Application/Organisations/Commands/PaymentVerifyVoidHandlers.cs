using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Payments;
using MediatR;

namespace FSBS.Application.Organisations.Commands;

public sealed class VerifyOrganisationPaymentHandler(
    IOrganisationAccountRepository accounts,
    ICurrentUser currentUser)
    : IRequestHandler<VerifyOrganisationPaymentCommand, PaymentDto>
{
    public Task<PaymentDto> Handle(VerifyOrganisationPaymentCommand request, CancellationToken ct) =>
        accounts.VerifyPaymentAsync(request.OrgId, request.PaymentId, currentUser.UserId, ct);
}

public sealed class VoidOrganisationPaymentHandler(
    IOrganisationAccountRepository accounts,
    ICurrentUser currentUser)
    : IRequestHandler<VoidOrganisationPaymentCommand, PaymentDto>
{
    public Task<PaymentDto> Handle(VoidOrganisationPaymentCommand request, CancellationToken ct) =>
        accounts.VoidPaymentAsync(request.OrgId, request.PaymentId, request.Reason, currentUser.UserId, ct);
}
