using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(GolfClubDbContext context, IPasswordHasher passwordHasher)
    {
        await context.Database.MigrateAsync();

        await SeedResourcesAsync(context);
        await SeedUsersAsync(context, passwordHasher);
    }

    private static async Task SeedResourcesAsync(GolfClubDbContext context)
    {
        if (await context.Resources.AnyAsync())
            return;

        var resources = new List<Resource>
        {
            new("6-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 10m),
            new("9-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 15m),
            new("18-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 22m),
            new("Driving Range Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(7, 0), new TimeOnly(21, 0)),
            new("Driving Range Bay 2", ResourceType.DrivingRangeBay, 60, new TimeOnly(7, 0), new TimeOnly(21, 0)),
            new("Golf Cart 1", ResourceType.GolfCart, 240, new TimeOnly(7, 0), new TimeOnly(19, 0)),
            new("Golf Cart 2", ResourceType.GolfCart, 240, new TimeOnly(7, 0), new TimeOnly(19, 0)),
            new("Lesson with Pro - Alex", ResourceType.LessonSlot, 45, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            new("Simulator Bay", ResourceType.Simulator, 60, new TimeOnly(8, 0), new TimeOnly(22, 0)),
        };

        await context.Resources.AddRangeAsync(resources);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(GolfClubDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync())
            return;

        var now = DateTime.Now;
        var users = new List<User>
        {
            new("Admin", "admin@testAdmin.com", passwordHasher.Hash("Admin"), now, isAdmin: true),
            new("Alice Smith", "alice@example.com", passwordHasher.Hash("Password123"), now),
            new("Bob Jones", "bob@example.com", passwordHasher.Hash("Password123"), now),
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}
