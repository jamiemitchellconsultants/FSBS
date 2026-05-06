using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Organisations.Queries;

/// <summary>Returns a name-sorted summary list of all active organisations.</summary>
public record ListOrganisationsQuery : IRequest<IReadOnlyList<OrganisationSummary>>;
