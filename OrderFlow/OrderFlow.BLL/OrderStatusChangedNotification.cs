using OrderFlow.Domain;

namespace OrderFlow.BLL;

// AD-4: carries exactly this shape, no more. Published only by OrderStatusService.TransitionTo,
// after the UnitOfWork commits the status change (never before).
public class OrderStatusChangedNotification
{
    public int OrderId { get; set; }
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
}
