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
        await SeedCartsAsync(context);
        await SeedUsersAsync(context, passwordHasher);
    }

    private static async Task SeedResourcesAsync(GolfClubDbContext context)
    {
        if (await context.Resources.AnyAsync())
            return;

        var sixHoleCourse = new Resource("6-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 10m);

        var resources = new List<Resource>
        {
            sixHoleCourse,
            new("9-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 15m),
            new("18-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 22m),
            new("Driving Range Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(7, 0), new TimeOnly(21, 0)),
            new("Driving Range Bay 2", ResourceType.DrivingRangeBay, 60, new TimeOnly(7, 0), new TimeOnly(21, 0)),
            // 1-hour slots, linked to the 6-Hole Course: booking a lesson also blocks that same
            // hour on the 6-Hole Course, and vice versa — see BookingService's bidirectional
            // overlap check.
            new("Lesson with Pro - Alex", ResourceType.LessonSlot, 60, new TimeOnly(9, 0), new TimeOnly(17, 0), linkedResourceId: sixHoleCourse.Id),
            // Three identical, independently-bookable simulator bays — grid slots are 1 hour wide,
            // but a single booking can span 1-5 of them (see BookingService's Simulator duration
            // check). Flat per-player price regardless of duration, same model as every other
            // priced resource.
            new("Simulator 1", ResourceType.Simulator, 60, new TimeOnly(8, 0), new TimeOnly(22, 0), pricePerPlayer: 20m),
            new("Simulator 2", ResourceType.Simulator, 60, new TimeOnly(8, 0), new TimeOnly(22, 0), pricePerPlayer: 20m),
            new("Simulator 3", ResourceType.Simulator, 60, new TimeOnly(8, 0), new TimeOnly(22, 0), pricePerPlayer: 20m),
        };

        await context.Resources.AddRangeAsync(resources);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCartsAsync(GolfClubDbContext context)
    {
        if (await context.Carts.AnyAsync())
            return;

        var carts = new List<Cart>
        {
            new("Cart 1"),
            new("Cart 2"),
            new("Cart 3"),
        };

        await context.Carts.AddRangeAsync(carts);
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
