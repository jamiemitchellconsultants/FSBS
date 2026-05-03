using FSBS.Application.Organisations.Queries;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Api.Endpoints;

public static class OrganisationEndpoints
{
    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/organisations")
            .WithTags("Organisations")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithName("ListOrganisations")
            .WithSummary("Return a name-sorted list of all active organisations.")
            .Produces<OrganisationListResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> ListAsync(ISender sender, CancellationToken ct)
    {
        var items = await sender.Send(new ListOrganisationsQuery(), ct);
        return Results.Ok(new OrganisationListResponse(items));
    }
}

public record OrganisationListResponse(IReadOnlyList<OrganisationSummary> Items);
