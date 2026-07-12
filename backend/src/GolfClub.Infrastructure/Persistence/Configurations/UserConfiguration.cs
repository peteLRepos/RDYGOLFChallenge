using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfClub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(User.MaxNameLength);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(User.MaxEmailLength);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.IsAdmin)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        // Backstops the application-layer uniqueness check against the same kind of race condition
        // documented for booking overlaps (see README).
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
