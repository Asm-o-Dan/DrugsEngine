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

        // Один препарат = название + производитель (одноимённые препараты разных производителей — разные).
        builder.HasIndex(d => new { d.Name, d.Manufacturer }).IsUnique();
        // Нечёткий поиск по названию (pg_trgm).
        builder.HasIndex(d => d.Name)
            .HasDatabaseName("IX_Drug_Name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        
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