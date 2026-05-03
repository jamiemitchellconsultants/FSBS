using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Infrastructure.Persistence.Repositories;

public static class RepositoriesServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        return services;
    }
}
