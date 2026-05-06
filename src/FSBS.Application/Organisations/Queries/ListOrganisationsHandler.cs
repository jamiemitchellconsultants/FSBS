using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Organisations.Queries;

public sealed class ListOrganisationsHandler(IOrganisationRepository organisations)
    : IRequestHandler<ListOrganisationsQuery, IReadOnlyList<OrganisationSummary>>
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<OrganisationSummary>> Handle(
        ListOrganisationsQuery request,
        CancellationToken ct) =>
        organisations.ListSummariesAsync(ct);
}
