namespace OrderFlow.Presentation;

// Presentation-only view model for the New Order line-item grid — never crosses into
// OrderFlow.BLL. UnitPriceAtOrder is snapshotted from ProductDto.UnitPrice at "Add Item"
// click time; the BLL processor trusts this value as-is (see OrderItem's Domain doc comment).
public class OrderLineItemRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceAtOrder { get; set; }

    public decimal LineTotal => Quantity * UnitPriceAtOrder;
}
