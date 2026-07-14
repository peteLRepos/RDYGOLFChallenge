using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfClub.Infrastructure.Persistence.Configurations;

public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("WaitlistEntries");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.SlotStart)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        // Backstops the application-layer duplicate-join check, same "domain check first, DB
        // constraint as concurrency backstop" pattern used for User.Email and booking overlaps.
        builder.HasIndex(w => new { w.ResourceId, w.SlotStart, w.UserId }).IsUnique();

        // Unidirectional, same reasoning as BookingPlayer's User link and Resource's own self-link:
        // neither Resource nor User has a delete path (only Deactivate), so Restrict is a backstop
        // that's never expected to actually fire.
        builder.HasOne(w => w.Resource)
            .WithMany()
            .HasForeignKey(w => w.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
