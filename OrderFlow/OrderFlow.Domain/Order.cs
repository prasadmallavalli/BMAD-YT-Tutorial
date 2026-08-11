namespace OrderFlow.Domain;

public class Order : IAuditable
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
