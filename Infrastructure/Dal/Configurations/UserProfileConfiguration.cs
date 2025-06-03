using Domain.Entities;
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
        builder.OwnsOne(up => up.Email, e =>
        {
            e.Property(ad => ad.Value).HasMaxLength(100);
        });
        
        builder.HasMany(up => up.FavoriteDrugs)
            .WithOne(fd => fd.UserProfile)
            .HasForeignKey(fd => fd.ProfileId);
    }
}