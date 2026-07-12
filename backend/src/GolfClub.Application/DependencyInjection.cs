using GolfClub.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GolfClub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
