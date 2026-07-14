using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfClub.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.SlotDurationMinutes)
            .IsRequired();

        builder.Property(r => r.OpeningTime)
            .IsRequired();

        builder.Property(r => r.ClosingTime)
            .IsRequired();

        // Nullable: most resource types aren't priced yet (see README assumptions) — null means
        // "no price set", distinct from a real (rejected) zero price.
        builder.Property(r => r.PricePerPlayer)
            .HasColumnType("decimal(10,2)");

        builder.Property(r => r.IsActive)
            .IsRequired();

        // Self-referencing, unidirectional (no inverse navigation needed — BookingService resolves
        // "who links to me" with a query, not a loaded collection). Restrict rather than Cascade:
        // there's no resource delete path today, so this is a backstop, not something expected to
        // fire in practice.
        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(r => r.LinkedResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
