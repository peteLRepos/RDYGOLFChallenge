using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfClub.Infrastructure.Persistence.Configurations;

public class BookingPlayerConfiguration : IEntityTypeConfiguration<BookingPlayer>
{
    public void Configure(EntityTypeBuilder<BookingPlayer> builder)
    {
        builder.ToTable("BookingPlayers");

        builder.HasKey(p => p.Id);

        // BookingPlayer is the first entity in this codebase that's only ever added via graph
        // discovery (appended to an already-tracked Booking's Players collection, never an
        // explicit context.Add()). Without this, EF Core can't tell a client-generated Guid key
        // apart from one that might already exist in the DB, and generates an UPDATE instead of
        // an INSERT for a genuinely new row — confirmed via a real DbUpdateConcurrencyException
        // ("expected to affect 1 row(s), but actually affected 0") when adding a second player.
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Handicap)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.AddedByUserId)
            .IsRequired();

        builder.Property(p => p.JoinedAt)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        // Shadow FK back to the owning Booking — BookingPlayer is a child entity within the
        // Booking aggregate (see its doc comment), never queried independently, so it doesn't
        // carry its own BookingId as a CLR property; it's always reached via Booking.Players.
        builder.Property<Guid>("BookingId")
            .IsRequired();

        // Backstop for the domain's no-double-joining check (Booking.AddPlayer), same
        // "domain check first, DB constraint as concurrency backstop" pattern already used for
        // User.Email and the booking-overlap index.
        builder.HasIndex("BookingId", nameof(BookingPlayer.UserId))
            .IsUnique();

        // Unidirectional, same reasoning as Booking.Booker: User has no delete path, only
        // Deactivate/Activate, so Restrict never fires in practice — a deliberate backstop.
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
