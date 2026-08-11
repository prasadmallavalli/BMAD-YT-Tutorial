using OrderFlow.DAL;

namespace OrderFlow.BLL;

public class InventoryService : IInventoryService
{
    private const string NotFoundError = "Product not found";

    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> GetStockLevelAsync(int productId)
    {
        var inventory = await _unitOfWork.Inventory.GetByProductIdAsync(productId);
        return inventory is null
            ? Result<int>.Failure(NotFoundError)
            : Result<int>.Success(inventory.StockQuantity);
    }

    // AD-13: the one and only place stock sufficiency is evaluated in this codebase.
    // Epic 2's OrderService calls this rather than reimplementing the comparison.
    public async Task<Result<bool>> HasSufficientStockAsync(int productId, int requestedQuantity)
    {
        if (requestedQuantity < 0)
        {
            return Result<bool>.Failure("Requested quantity cannot be negative");
        }

        var inventory = await _unitOfWork.Inventory.GetByProductIdAsync(productId);
        return inventory is null
            ? Result<bool>.Failure(NotFoundError)
            : Result<bool>.Success(inventory.StockQuantity >= requestedQuantity);
    }
}
