using OrderFlow.Domain;

namespace OrderFlow.BLL;

public class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public OrderType OrderType { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}
