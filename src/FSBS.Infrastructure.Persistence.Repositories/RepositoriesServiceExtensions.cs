using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// DI registration extension for all repository implementations.
/// Registers both read-side repositories (from <c>Repositories.Interfaces</c>)
/// and write-side repositories (from <c>Domain.Interfaces</c>).
/// </summary>
public static class RepositoriesServiceExtensions
{
    /// <summary>
    /// Adds all read-side and write-side repository implementations as scoped services.
    /// Call this from <c>FSBS.Api/Program.cs</c> after <c>AddPersistence</c>.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Read-side repositories — implement interfaces from Repositories.Interfaces
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ILessonTemplateReadRepository, LessonTemplateReadRepository>();

        // Write-side repositories — implement interfaces from Domain.Interfaces
        services.AddScoped<Domain.Interfaces.IBookingRepository, BookingWriteRepository>();
        services.AddScoped<Domain.Interfaces.ISimulatorRepository, SimulatorRepository>();
        services.AddScoped<Domain.Interfaces.IReconfigurationTemplateRepository, ReconfigurationTemplateRepository>();
        services.AddScoped<Domain.Interfaces.IReconfigurationSlotRepository, ReconfigurationSlotRepository>();
        services.AddScoped<Domain.Interfaces.IPricingPolicyRepository, PricingPolicyRepository>();
        services.AddScoped<Domain.Interfaces.IInstructorRepository, InstructorRepository>();
        services.AddScoped<Domain.Interfaces.IAircraftTypeRepository, AircraftTypeRepository>();
        services.AddScoped<Domain.Interfaces.ILessonTemplateRepository, LessonTemplateRepository>();
        services.AddScoped<Domain.Interfaces.ILessonRepository, LessonRepository>();
        services.AddScoped<Application.Common.Interfaces.IAvailabilityReadService, AvailabilityReadService>();
        services.AddScoped<Application.Common.Interfaces.IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<Application.Common.Interfaces.IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<Application.Common.Interfaces.IInstructorScheduleRepository, InstructorScheduleRepository>();
        services.AddScoped<Application.Common.Interfaces.IOrganisationAccountRepository, OrganisationAccountRepository>();

        return services;
    }
}
