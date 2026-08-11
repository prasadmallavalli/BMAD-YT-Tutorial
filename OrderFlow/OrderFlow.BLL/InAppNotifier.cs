namespace OrderFlow.BLL;

// Minimal in-app-log INotifier (AC #2). Singleton lifetime (AD-5) means this instance is shared
// across every concurrently-running business operation, so the log list is lock-guarded — two
// near-simultaneous TransitionTo calls (plausible given NFR-1's async-everywhere requirement and
// the codebase's already-deferred "no busy indicator" gap) must not corrupt each other's writes.
public class InAppNotifier : INotifier
{
    private readonly List<NotificationLogEntry> _log = [];
    private readonly object _lock = new();

    public event EventHandler<NotificationLogEntry>? Notified;

    public void Notify(OrderStatusChangedNotification notification)
    {
        var entry = new NotificationLogEntry { Notification = notification, OccurredAtUtc = DateTime.UtcNow };

        // Append and raise inside the same lock: two near-simultaneous Notify() calls must not
        // only append to _log in a consistent order but also fire Notified in that same order —
        // AC #2's "listed in order" only holds if a live subscriber can never observe entries
        // out of _log's own append order. This does mean a slow/reentrant subscriber can briefly
        // hold up a concurrent TransitionTo call's own Notify() (a stronger guarantee traded
        // deliberately for correct ordering); MainForm's only subscriber (Notifier_Notified)
        // stays cheap by immediately marshaling to the UI thread via BeginInvoke rather than
        // doing any work inline.
        lock (_lock)
        {
            _log.Add(entry);

            // A subscriber's fault must never fail an already-committed TransitionTo — the log
            // append above already succeeded, so a broken UI-side handler is swallowed here
            // rather than surfaced to the caller of Notify().
            try
            {
                Notified?.Invoke(this, entry);
            }
            catch
            {
                // Intentionally swallowed — see comment above.
            }
        }
    }

    public IReadOnlyList<NotificationLogEntry> GetLog()
    {
        lock (_lock)
        {
            return _log.ToList();
        }
    }
}
