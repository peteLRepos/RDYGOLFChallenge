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

        builder.Property(b => b.IsPaid)
            .IsRequired();

        // Persists whether IsPaid was set via an explicit MarkPaid() call, so RecomputeIsPaid
        // (run on every AddPlayer/RemovePlayer) knows never to overwrite a manual settlement —
        // see Booking.RecomputeIsPaid.
        builder.Property<bool>("_manuallyMarkedPaid")
            .HasColumnName("ManuallyMarkedPaid")
            .IsRequired();

        // Derived from Players.Count/Players.Sum(...) — not persisted columns, not settable, so
        // they'd otherwise trip up EF's convention-based model discovery.
        builder.Ignore(b => b.PlayerCount);
        builder.Ignore(b => b.CombinedHandicap);

        // Snapshotted at booking time (see Booking's constructor doc) rather than derived live
        // from Resource.PricePerPlayer, so a later price change doesn't alter what an existing
        // booking was charged.
        builder.Property(b => b.TotalPrice)
            .HasColumnType("decimal(10,2)")
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

        // A booking's players don't outlive the booking, unlike the Booker/Resource FKs above
        // (which use Restrict) — Cascade is correct here since BookingPlayer only exists as part
        // of this aggregate.
        builder.HasMany(b => b.Players)
            .WithOne()
            .HasForeignKey("BookingId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Players)
            .HasField("_players")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
