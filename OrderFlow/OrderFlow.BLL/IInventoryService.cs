namespace OrderFlow.BLL;

public interface IInventoryService
{
    Task<Result<int>> GetStockLevelAsync(int productId);
    Task<Result<bool>> HasSufficientStockAsync(int productId, int requestedQuantity);
}
