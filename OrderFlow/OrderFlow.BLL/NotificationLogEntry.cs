namespace OrderFlow.BLL;

// Wraps a notification with the timestamp Story 3.4's notification panel needs — kept off
// OrderStatusChangedNotification itself, which AD-4 pins to exactly OrderId/OldStatus/NewStatus.
public class NotificationLogEntry
{
    public OrderStatusChangedNotification Notification { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; }
}
