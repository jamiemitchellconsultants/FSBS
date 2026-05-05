using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Infrastructure.Persistence.Repositories;

public static class RepositoriesServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Existing repositories — implement interfaces from Repositories.Interfaces
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Phase 3 repositories — implement interfaces from Domain.Interfaces
        services.AddScoped<Domain.Interfaces.ISimulatorRepository, SimulatorRepository>();
        services.AddScoped<Domain.Interfaces.IReconfigurationTemplateRepository, ReconfigurationTemplateRepository>();
        services.AddScoped<Domain.Interfaces.IReconfigurationSlotRepository, ReconfigurationSlotRepository>();
        services.AddScoped<Domain.Interfaces.IPricingPolicyRepository, PricingPolicyRepository>();
        services.AddScoped<Domain.Interfaces.IInstructorRepository, InstructorRepository>();
        services.AddScoped<Application.Common.Interfaces.IAvailabilityReadService, AvailabilityReadService>();

        return services;
    }
}
