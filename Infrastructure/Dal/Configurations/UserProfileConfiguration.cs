using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Dal.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable(nameof(UserProfile));
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).IsRequired();

        builder.Property(up => up.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(up => up.ExternalId)
            .IsUnique();

        builder.Property(up => up.Email)
            .HasConversion(
                email => email != null ? email.Value : null,
                value => value != null ? new Email(value) : null
            )
            .HasMaxLength(100);
        
        builder.HasMany(up => up.FavoriteDrugs)
            .WithOne(fd => fd.UserProfile)
            .HasForeignKey(fd => fd.ProfileId);
    }
}