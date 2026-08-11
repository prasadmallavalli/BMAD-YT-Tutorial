using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

public class OrderService : IOrderService
{
    private const string NotFoundError = "Order not found";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingStrategy _pricingStrategy;

    public OrderService(IUnitOfWork unitOfWork, IPricingStrategy pricingStrategy)
    {
        _unitOfWork = unitOfWork;
        _pricingStrategy = pricingStrategy;
    }

    public async Task<Result<OrderDto>> GetAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        if (order is null)
        {
            return Result<OrderDto>.Failure(NotFoundError);
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(order.CustomerId);

        // Only the products this order actually references — not the whole catalog. Unlike
        // GetAllAsync (which batches once across every Order, so the full catalog is fetched
        // only once regardless of order count), a single order's product set is small and
        // bounded by its own line-item count, so a per-product lookup here doesn't reintroduce
        // the N+1-across-orders pattern GetAllAsync avoids.
        var productNamesById = new Dictionary<int, string>();
        foreach (var productId in order.OrderItems.Select(item => item.ProductId).Distinct())
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product is not null)
            {
                productNamesById[productId] = product.Name;
            }
        }

        return Result<OrderDto>.Success(ToDto(order, customer?.Name ?? string.Empty, productNamesById));
    }

    // Batch-fetches Customers/Products once, not once per Order — Order has no Customer/Product
    // navigation property (OrderConfiguration.cs, Story 2.1), so this is the join.
    public async Task<Result<IReadOnlyList<OrderDto>>> GetAllAsync()
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var products = await _unitOfWork.Products.GetAllAsync();

        var customerNamesById = customers.ToDictionary(c => c.Id, c => c.Name);
        var productNamesById = products.ToDictionary(p => p.Id, p => p.Name);

        var dtos = orders
            .Select(order => ToDto(order, customerNamesById.GetValueOrDefault(order.CustomerId, string.Empty), productNamesById))
            .ToList();

        return Result<IReadOnlyList<OrderDto>>.Success(dtos);
    }

    // Total isn't stored on Order (Story 2.1) — recomputed from OrderItems' UnitPriceAtOrder
    // snapshots via the same calculation Story 2.5's processors use (OrderTotalCalculator).
    private OrderDto ToDto(Order order, string customerName, IReadOnlyDictionary<int, string> productNamesById)
    {
        var items = order.OrderItems.Select(item => new OrderItemDto
        {
            ProductId = item.ProductId,
            ProductName = productNamesById.GetValueOrDefault(item.ProductId, string.Empty),
            Quantity = item.Quantity,
            UnitPriceAtOrder = item.UnitPriceAtOrder
        }).ToList();

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customerName,
            OrderType = order.OrderType,
            Status = order.Status,
            Total = OrderTotalCalculator.Calculate(order.OrderType, _pricingStrategy, items),
            Items = items
        };
    }
}
