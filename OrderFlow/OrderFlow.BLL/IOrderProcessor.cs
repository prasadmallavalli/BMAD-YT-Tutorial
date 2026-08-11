namespace OrderFlow.BLL;

public interface IOrderProcessor
{
    // Validates stock for every line item (AD-13), computes the total (Standard: base total;
    // Rush: base total + 10% surcharge), persists the Order+OrderItems, decrements Inventory,
    // and transitions status to Confirmed via IOrderStatusService (Story 2.5) — the processor
    // itself never sets Confirmed directly (AD-4). Returns a failure Result without persisting
    // anything if any line item has insufficient stock.
    Task<Result<OrderDto>> ConfirmAsync(CreateOrderRequest request);
}
