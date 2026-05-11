using MediatR;
using FSBS.Shared.Payments;

namespace FSBS.Application.Organisations.Queries;

public record GetOrganisationAccountQuery(Guid OrgId) : IRequest<OrgAccountDto?>;

