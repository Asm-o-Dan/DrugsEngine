using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Dal.Configurations;


/// <summary>
/// Конфигурация для сущности <see cref="FavoriteDrug"/>
/// </summary>
public class FavoriteDrugConfiguration:IEntityTypeConfiguration<FavoriteDrug>
{
    public void Configure(EntityTypeBuilder<FavoriteDrug> builder)
    {
        builder.ToTable(nameof(FavoriteDrug));

        builder.Property(fd => fd.ProfileId).IsRequired();

        builder.Property(fd => fd.DrugId).IsRequired();

        builder.Property(fd => fd.DrugStoreId).IsRequired();
        
        builder.HasKey(fd => new { fd.ProfileId, fd.DrugId, fd.DrugStoreId });

        builder.HasOne(fd => fd.UserProfile)
            .WithMany(up => up.FavoriteDrugs)
            .HasForeignKey(fd => fd.ProfileId);

        builder.HasOne(fd => fd.Drug)
            .WithMany()
            .HasForeignKey(fd => fd.DrugId);

        builder.HasOne(fd => fd.DrugStore)
            .WithMany()
            .HasForeignKey(fd => fd.DrugStoreId);
    }
}