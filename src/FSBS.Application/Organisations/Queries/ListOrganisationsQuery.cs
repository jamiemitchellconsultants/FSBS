using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Organisations.Queries;

public record ListOrganisationsQuery : IRequest<IReadOnlyList<OrganisationSummary>>;
