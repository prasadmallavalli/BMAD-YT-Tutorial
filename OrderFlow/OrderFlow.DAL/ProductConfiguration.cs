using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.UnitPrice)
            .HasColumnType("decimal(18,2)");

        // 1:1 — unidirectional (Inventory has no back-navigation, just the FK scalar).
        // WithOne() + HasForeignKey<Inventory> makes EF Core enforce this via a unique
        // index on Inventory.ProductId, not just "1:many with an unused nav" (Story 1.4
        // Dev Notes).
        builder.HasOne(p => p.Inventory)
            .WithOne()
            .HasForeignKey<Inventory>(i => i.ProductId);
    }
}
