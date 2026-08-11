namespace OrderFlow.BLL;

// AD-4/AD-5: registered Singleton — one of the two services AD-5 reserves that lifetime for
// (alongside the not-yet-built IAppSettings). Notify is called exclusively by
// OrderStatusService.TransitionTo, after the UnitOfWork commits.
public interface INotifier
{
    void Notify(OrderStatusChangedNotification notification);

    // Story 3.4's notification panel reads this — populated here in Story 2.4 even though
    // nothing calls it yet, so INotifier's shape doesn't need to change later.
    IReadOnlyList<NotificationLogEntry> GetLog();

    // Story 3.4: the live-subscription counterpart to GetLog()'s point-in-time snapshot — the
    // Singleton lifetime exists specifically so a UI-side subscriber can attach once and keep
    // receiving entries fired after that point, without polling GetLog() again.
    event EventHandler<NotificationLogEntry>? Notified;
}
