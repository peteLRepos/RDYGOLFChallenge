using GolfClub.Application.Interfaces;
using GolfClub.Infrastructure.Persistence;
using GolfClub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GolfClub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<GolfClubDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
