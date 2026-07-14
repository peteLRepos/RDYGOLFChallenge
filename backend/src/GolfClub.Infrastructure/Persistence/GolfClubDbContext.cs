using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence;

public class GolfClubDbContext : DbContext
{
    public GolfClubDbContext(DbContextOptions<GolfClubDbContext> options) : base(options)
    {
    }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Cart> Carts => Set<Cart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GolfClubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
