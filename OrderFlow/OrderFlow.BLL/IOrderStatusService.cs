using OrderFlow.Domain;

namespace OrderFlow.BLL;

public interface IOrderStatusService
{
    // Named to match AD-4's literal wording (no "Async" suffix, unlike this codebase's other
    // async BLL methods) — this is the exact method future stories (3.1, 3.3) call by name.
    Task<Result<OrderStatus>> TransitionTo(int orderId, OrderStatus newStatus);

    // Read accessor onto the same AllowedTransitions table TransitionTo consults — synchronous,
    // no DB access. Lets Presentation know which statuses to offer (Story 3.3, AC #1) without
    // duplicating the table anywhere outside this service (AD-4's sole-owner rule).
    IReadOnlyList<OrderStatus> GetAllowedNextStatuses(OrderType orderType, OrderStatus currentStatus);
}
