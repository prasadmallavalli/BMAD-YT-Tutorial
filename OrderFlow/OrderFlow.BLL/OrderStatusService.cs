using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

// AD-4: sole owner of the allowed-transition table and the only caller of INotifier.Notify.
// Full per-OrderType lifecycle (Story 3.1), locked verbatim by epics.md's Epic 3 decision:
// Standard: Confirmed -> Processing -> Shipped -> Delivered, Cancelled reachable from Confirmed
// or Processing (not after Shipped). Rush: same forward sequence, but Cancelled is reachable
// only from Confirmed — once Processing starts, a Rush order is committed. Delivered/Cancelled
// are terminal (no entry) — a status with no entry in its OrderType partition already fails
// correctly via the existing TryGetValue miss path.
public class OrderStatusService : IOrderStatusService
{
    private const string NotFoundError = "Order not found";

    private static readonly IReadOnlyDictionary<OrderType, IReadOnlyDictionary<OrderStatus, OrderStatus[]>> AllowedTransitions =
        new Dictionary<OrderType, IReadOnlyDictionary<OrderStatus, OrderStatus[]>>
        {
            [OrderType.Standard] = new Dictionary<OrderStatus, OrderStatus[]>
            {
                [OrderStatus.Unspecified] = [OrderStatus.Confirmed],
                [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
                [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
                [OrderStatus.Shipped] = [OrderStatus.Delivered]
            },
            [OrderType.Rush] = new Dictionary<OrderStatus, OrderStatus[]>
            {
                [OrderStatus.Unspecified] = [OrderStatus.Confirmed],
                [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
                [OrderStatus.Processing] = [OrderStatus.Shipped],
                [OrderStatus.Shipped] = [OrderStatus.Delivered]
            }
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotifier _notifier;

    public OrderStatusService(IUnitOfWork unitOfWork, INotifier notifier)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async Task<Result<OrderStatus>> TransitionTo(int orderId, OrderStatus newStatus)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order is null)
        {
            return Result<OrderStatus>.Failure(NotFoundError);
        }

        if (!TryGetAllowedTransitions(order.OrderType, order.Status, out var allowedNextStatuses) ||
            !allowedNextStatuses.Contains(newStatus))
        {
            return Result<OrderStatus>.Failure($"Cannot transition Order {orderId} from {order.Status} to {newStatus}");
        }

        var oldStatus = order.Status;

        // Mutate the tracked entity directly — EF's change tracker marks only Status Modified.
        // Never reconstruct/reattach a detached graph (AD-6).
        order.Status = newStatus;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (ConcurrencyConflictException ex)
        {
            // AD-10: translate the concurrency conflict into a Result<T> failure — never let it
            // reach Presentation unhandled. Same pattern as ProductService.UpdateAsync.
            return Result<OrderStatus>.Failure(ex.Message);
        }

        // AD-4: notify only after the UnitOfWork confirms the status change persisted.
        _notifier.Notify(new OrderStatusChangedNotification
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });

        return Result<OrderStatus>.Success(newStatus);
    }

    // Shared lookup chain both TransitionTo and GetAllowedNextStatuses consult — a single
    // source of truth for the miss-path semantics (empty/false for an unknown OrderType or a
    // terminal/unmapped currentStatus, e.g. Delivered/Cancelled).
    private static bool TryGetAllowedTransitions(OrderType orderType, OrderStatus currentStatus, out OrderStatus[] allowed)
    {
        if (AllowedTransitions.TryGetValue(orderType, out var transitionsForType) &&
            transitionsForType.TryGetValue(currentStatus, out var allowedNextStatuses))
        {
            allowed = allowedNextStatuses;
            return true;
        }

        allowed = [];
        return false;
    }

    // Returns a copy, not the array instance backing the static AllowedTransitions table —
    // callers only see IReadOnlyList<OrderStatus>, but a defensive copy still protects the
    // shared table from a caller that casts back to OrderStatus[] and mutates it.
    public IReadOnlyList<OrderStatus> GetAllowedNextStatuses(OrderType orderType, OrderStatus currentStatus) =>
        TryGetAllowedTransitions(orderType, currentStatus, out var allowed) ? allowed.ToArray() : [];
}
