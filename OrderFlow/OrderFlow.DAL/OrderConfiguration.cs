using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain;

namespace OrderFlow.DAL;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        // FK only, no nav on either side — Customer gets no Orders collection (mirrors
        // OrderItem's unidirectional relationship with Product, not Product/Inventory's
        // one-sided-nav 1:1, which is a different shape).
        // Restrict, not the EF default Cascade: AD-6 forbids soft-delete, so hard-delete is
        // the only path a future Customer-delete feature could take, and it must not be able
        // to silently erase Order history (code review, Story 2.1).
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
