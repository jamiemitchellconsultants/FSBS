using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Payments;
using MediatR;

namespace FSBS.Application.Organisations.Queries;

public sealed class GetOrganisationAccountHandler(IOrganisationAccountRepository accounts)
    : IRequestHandler<GetOrganisationAccountQuery, OrgAccountDto?>
{
    public Task<OrgAccountDto?> Handle(GetOrganisationAccountQuery request, CancellationToken ct) =>
        accounts.GetAccountAsync(request.OrgId, ct);
}

