using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class InAppNotifierTests
{
    [Fact]
    public void Notify_AppendsEntryToLogWithNotificationAndTimestamp()
    {
        var notifier = new InAppNotifier();
        var notification = new OrderStatusChangedNotification
        {
            OrderId = 1,
            OldStatus = OrderStatus.Unspecified,
            NewStatus = OrderStatus.Confirmed
        };
        var before = DateTime.UtcNow;

        notifier.Notify(notification);

        var after = DateTime.UtcNow;
        var log = notifier.GetLog();
        Assert.Single(log);
        Assert.Same(notification, log[0].Notification);
        Assert.InRange(log[0].OccurredAtUtc, before, after);
    }

    [Fact]
    public void Notify_CalledMultipleTimes_AppendsEntriesInOrder()
    {
        var notifier = new InAppNotifier();
        var first = new OrderStatusChangedNotification { OrderId = 1, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed };
        var second = new OrderStatusChangedNotification { OrderId = 2, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed };

        notifier.Notify(first);
        notifier.Notify(second);

        var log = notifier.GetLog();
        Assert.Equal(2, log.Count);
        Assert.Same(first, log[0].Notification);
        Assert.Same(second, log[1].Notification);
    }

    [Fact]
    public void GetLog_ReturnsDefensiveSnapshot_NotLiveList()
    {
        var notifier = new InAppNotifier();
        notifier.Notify(new OrderStatusChangedNotification { OrderId = 1, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed });

        var firstSnapshot = notifier.GetLog();
        notifier.Notify(new OrderStatusChangedNotification { OrderId = 2, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed });

        // The snapshot taken before the second Notify() must not observe it.
        Assert.Single(firstSnapshot);
        Assert.Equal(2, notifier.GetLog().Count);
    }

    [Fact]
    public void GetLog_WithNoNotifications_ReturnsEmpty()
    {
        var notifier = new InAppNotifier();

        Assert.Empty(notifier.GetLog());
    }

    [Fact]
    public void Notify_RaisesNotifiedEventWithTheAppendedEntry()
    {
        var notifier = new InAppNotifier();
        NotificationLogEntry? raised = null;
        notifier.Notified += (_, entry) => raised = entry;
        var notification = new OrderStatusChangedNotification { OrderId = 1, OldStatus = OrderStatus.Confirmed, NewStatus = OrderStatus.Processing };

        notifier.Notify(notification);

        Assert.NotNull(raised);
        Assert.Same(notification, raised!.Notification);
        Assert.Same(notifier.GetLog()[0], raised);
    }

    [Fact]
    public void Notify_CalledMultipleTimes_RaisesNotifiedEventEachTimeInOrder()
    {
        var notifier = new InAppNotifier();
        var raised = new List<NotificationLogEntry>();
        notifier.Notified += (_, entry) => raised.Add(entry);
        var first = new OrderStatusChangedNotification { OrderId = 1, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed };
        var second = new OrderStatusChangedNotification { OrderId = 2, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed };

        notifier.Notify(first);
        notifier.Notify(second);

        Assert.Equal(2, raised.Count);
        Assert.Same(first, raised[0].Notification);
        Assert.Same(second, raised[1].Notification);
    }

    [Fact]
    public void Notify_WithNoSubscribers_StillAppendsToLog()
    {
        var notifier = new InAppNotifier();

        var exception = Record.Exception(() => notifier.Notify(
            new OrderStatusChangedNotification { OrderId = 1, OldStatus = OrderStatus.Unspecified, NewStatus = OrderStatus.Confirmed }));

        Assert.Null(exception);
        Assert.Single(notifier.GetLog());
    }
}
