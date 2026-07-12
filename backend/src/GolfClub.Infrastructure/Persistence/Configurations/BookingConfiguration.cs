using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfClub.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.CustomerEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Bookings are stored as naive local timestamps (no timezone handling in this scope — see README).
        builder.Property(b => b.Start).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(b => b.End).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(b => b.CreatedAt).HasColumnType("timestamp without time zone").IsRequired();

        // Speeds up the overlap-check query the application layer runs on every booking attempt.
        builder.HasIndex(b => new { b.ResourceId, b.Start, b.End });
    }
}
