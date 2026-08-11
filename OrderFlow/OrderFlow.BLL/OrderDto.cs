using OrderFlow.Domain;

namespace OrderFlow.BLL;

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    // Display-only enrichment for list/detail views (Story 3.2) — populated by OrderService
    // via a batch Customer lookup, not an EF navigation property (Order has none, by design).
    public string CustomerName { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = [];
}
