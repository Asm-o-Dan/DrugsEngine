using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Dal.Configurations;

public class DrugConfiguration: IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.ToTable(nameof(Drug));
        
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).IsRequired();
        
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Manufacturer).HasMaxLength(100).IsRequired();
        builder.Property(d => d.CountryCodeId).HasMaxLength(2).IsRequired();
        
        builder.HasOne(d => d.Country)
            .WithMany(c => c.Drugs) 
            .HasForeignKey(d => d.CountryCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(d => d.DrugItems)
            .WithOne(d => d.Drug)
            .HasForeignKey(d => d.DrugId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}