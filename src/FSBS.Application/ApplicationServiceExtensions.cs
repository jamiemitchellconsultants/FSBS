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

public static class ApplicationServiceExtensions
{
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
