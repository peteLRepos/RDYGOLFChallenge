using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(GolfClubDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Resources.AnyAsync())
            return;

        var resources = new List<Resource>
        {
            new("Tee Time - Hole 1", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0)),
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
}
