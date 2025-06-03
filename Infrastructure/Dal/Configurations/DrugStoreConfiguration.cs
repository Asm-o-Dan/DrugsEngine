using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Dal.Configurations;

public class DrugStoreConfiguration: IEntityTypeConfiguration<DrugStore>
{
    public void Configure(EntityTypeBuilder<DrugStore> builder)
    {
        builder.ToTable(nameof(DrugStore));
        
        builder.HasKey(ds => ds.Id);
        builder.Property(ds => ds.Id).IsRequired();
        
        builder.Property(ds => ds.Number).IsRequired();
        builder.Property(ds => ds.PhoneNumber).IsRequired();
        builder.OwnsOne(d => d.Address, a =>
        {
            a.Property(ad => ad.City).IsRequired().HasMaxLength(50);
            a.Property(ad => ad.Street).IsRequired().HasMaxLength(100);
            a.Property(ad => ad.House).IsRequired().HasMaxLength(50);
        });
        builder.Property(ds => ds.DrugNetwork).IsRequired().HasMaxLength(100);

        builder.HasMany(ds => ds.DrugItems)
            .WithOne(di => di.DrugStore)
            .HasForeignKey(di => di.DrugStoreId);
    }
}