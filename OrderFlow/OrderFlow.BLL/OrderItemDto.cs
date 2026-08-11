namespace OrderFlow.BLL;

public class OrderItemDto
{
    public int ProductId { get; set; }
    // Display-only enrichment for list/detail views (Story 3.2) — populated by OrderService
    // via a batch Product lookup, not an EF navigation property (OrderItem has none, by design).
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceAtOrder { get; set; }
}
