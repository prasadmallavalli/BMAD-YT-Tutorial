using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

// AD-7: resolved via keyed DI for OrderType.Rush. Confirms an order: validates stock,
// computes the total (base total + 10% rush surcharge), persists Order+OrderItems,
// decrements Inventory, then transitions status to Confirmed via OrderStatusService (Story 2.5).
public class RushOrderProcessor : IOrderProcessor
{
    private readonly IPricingStrategy _pricingStrategy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly IOrderStatusService _orderStatusService;

    public RushOrderProcessor(
        IPricingStrategy pricingStrategy,
        IUnitOfWork unitOfWork,
        IInventoryService inventoryService,
        IOrderStatusService orderStatusService)
    {
        _pricingStrategy = pricingStrategy;
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _orderStatusService = orderStatusService;
    }

    public async Task<Result<OrderDto>> ConfirmAsync(CreateOrderRequest request)
    {
        // Step 1: stock check per distinct product — combined quantity across duplicate line
        // items for the same Product, not checked independently — no mutation yet (AD-13, AC #3).
        var insufficientProductIds = new List<int>();
        var quantityByProductId = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        foreach (var (productId, quantity) in quantityByProductId)
        {
            var stockResult = await _inventoryService.HasSufficientStockAsync(productId, quantity);
            if (!stockResult.IsSuccess || !stockResult.Value)
            {
                insufficientProductIds.Add(productId);
            }
        }

        if (insufficientProductIds.Count > 0)
        {
            return Result<OrderDto>.Failure(
                $"Insufficient stock for product(s): {string.Join(", ", insufficientProductIds)}");
        }

        // Step 2: compute total — base total + 10% rush surcharge (OrderTotalCalculator).
        var total = OrderTotalCalculator.Calculate(request.OrderType, _pricingStrategy, request.Items);

        // Step 3: persist Order + OrderItems (cascade insert, single SaveChangesAsync — Story 2.1).
        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderType = request.OrderType,
            Status = OrderStatus.Unspecified,
            OrderItems = request.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPriceAtOrder = item.UnitPriceAtOrder
            }).ToList()
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Step 4: decrement Inventory per distinct product — mutate tracked entities directly
        // (AD-6), guarding against a missing row or a quantity that became insufficient since
        // Step 1's check (e.g. a near-simultaneous confirm on the same product already
        // committed) — RowVersion concurrency alone only catches a stale-read-then-write race,
        // not a fresh-read-then-oversell sequence. Then a separate concurrency-guarded save
        // (AD-10, NFR-2).
        foreach (var (productId, quantity) in quantityByProductId)
        {
            var inventory = await _unitOfWork.Inventory.GetByProductIdAsync(productId);
            if (inventory is null || inventory.StockQuantity < quantity)
            {
                return Result<OrderDto>.Failure($"Insufficient stock for product(s): {productId}");
            }

            inventory.StockQuantity -= quantity;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }

        // Step 5: transition status to Confirmed — sole caller of INotifier.Notify (AD-4),
        // already concurrency-guarded internally (Story 2.4).
        var transitionResult = await _orderStatusService.TransitionTo(order.Id, OrderStatus.Confirmed);
        if (!transitionResult.IsSuccess)
        {
            return Result<OrderDto>.Failure(transitionResult.Error!);
        }

        return Result<OrderDto>.Success(new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderType = order.OrderType,
            Status = transitionResult.Value,
            Total = total,
            Items = request.Items
        });
    }
}
