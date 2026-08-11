using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.UnitPriceAtOrder)
            .HasColumnType("decimal(18,2)");

        // FK only, no nav either side — Product gets no OrderItems collection.
        // Restrict, not the EF default Cascade: UnitPriceAtOrder is a historical price
        // snapshot, and AD-6 forbids soft-delete, so a future Product-delete feature must not
        // be able to silently erase it via cascade (code review, Story 2.1).
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order-side nav is required for the cascade-insert (AC #2) — OrderItem has no
        // back-nav to Order.
        builder.HasOne<Order>()
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);
    }
}
