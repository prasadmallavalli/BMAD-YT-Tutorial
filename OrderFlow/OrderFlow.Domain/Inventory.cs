namespace OrderFlow.Domain;

public class Inventory : IAuditable
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StockQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
