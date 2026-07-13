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

        builder.Property(b => b.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.IsPaid)
            .IsRequired();

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

        // Speeds up looking up a user's own bookings (BookingService.GetMyBookingsAsync).
        builder.HasIndex(b => b.BookerId);

        // Unidirectional: Resource doesn't expose a Bookings collection (nothing in the codebase
        // needs to navigate resource -> bookings; BookingRepository queries Bookings directly).
        builder.HasOne(b => b.Resource)
            .WithMany()
            .HasForeignKey(b => b.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unidirectional, same navigation reasoning as Resource above. Restrict rather than Cascade:
        // User has no delete path (only Deactivate/Activate, see UserService), so this never fires
        // in practice — it's a deliberate backstop against a user row disappearing and silently
        // taking booking history with it, should that ever change.
        builder.HasOne(b => b.Booker)
            .WithMany()
            .HasForeignKey(b => b.BookerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
