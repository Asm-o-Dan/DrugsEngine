using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Dal.Configurations;

public class DrugItemConfiguration:IEntityTypeConfiguration<DrugItem>
{
    public void Configure(EntityTypeBuilder<DrugItem> builder)
    {
        builder.ToTable(nameof(DrugItem));
        
        builder.Property(d => d.Id).IsRequired();
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Cost);
        builder.Property(d => d.Count);

        builder.HasOne(d => d.Drug)
            .WithMany(d => d.DrugItems)
            .HasForeignKey(di => di.DrugId);

        builder.HasOne(d => d.DrugStore)
            .WithMany(d => d.DrugItems)
            .HasForeignKey(d => d.DrugStoreId);
    }
}