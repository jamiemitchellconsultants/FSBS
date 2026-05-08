using FSBS.Application.Bookings.Services;
using FSBS.Application.Common.Behaviours;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.Common.Services;
using FSBS.Application.Pricing.Services;
using FSBS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Application;

/// <summary>
/// DI registration extension for the Application layer.
/// Registers MediatR with the three pipeline behaviours, FluentValidation validators,
/// and application-layer services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds MediatR (with <c>LoggingBehaviour</c>, <c>ValidationBehaviour</c>, and
    /// <c>TransactionBehaviour</c>), FluentValidation validators, and scoped
    /// application services to the service collection.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Behaviours execute in registration order.
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IReconfigurationService, ReconfigurationService>();

        return services;
    }
}
