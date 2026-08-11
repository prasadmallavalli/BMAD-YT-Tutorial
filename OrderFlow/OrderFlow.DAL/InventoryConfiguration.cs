using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        // Explicit singular table name — EF Core doesn't auto-pluralize by default,
        // but naming it explicitly avoids any ambiguity about the deliberate choice.
        builder.ToTable("Inventory");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.RowVersion)
            .IsRowVersion();
    }
}
