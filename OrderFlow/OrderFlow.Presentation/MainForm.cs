using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

// Minimal shell — no business logic (Story 1.1). Composition root wiring compiles
// and passes ServiceProvider validation; runtime launch on Windows remains
// UNVERIFIED-ENVIRONMENT — see Story 1.1 Task 5 / Completion Notes.
// Root IServiceProvider injected solely to launch other Transient Forms — never to
// resolve BLL/DAL services directly. See Story 1.3 Dev Notes.
//
// Story 3.4: also constructor-injects INotifier directly — a second, narrow AD-3 exception
// alongside the IServiceProvider one above. INotifier is one of only two Singletons in the app
// (AD-5) and this Form only observes it to render already-computed fields; it never calls a BLL
// method that performs validation, pricing, or workflow logic, so it doesn't need a Presenter's
// per-operation IServiceScope the way a real business operation would.
public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotifier _notifier;
    private readonly BindingList<NotificationRow> _notifications;

    public MainForm(IServiceProvider serviceProvider, INotifier notifier)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _notifier = notifier;

        _notifications = new BindingList<NotificationRow>(notifier.GetLog().Select(MapToRow).ToList());
        notificationDataGridView.DataSource = _notifications;
        _notifier.Notified += Notifier_Notified;
    }

    private void CustomersButton_Click(object? sender, EventArgs e)
    {
        var listForm = _serviceProvider.GetRequiredService<CustomerListForm>();
        listForm.Show(this);
    }

    private void ProductsButton_Click(object? sender, EventArgs e)
    {
        var listForm = _serviceProvider.GetRequiredService<ProductListForm>();
        listForm.Show(this);
    }

    private void NewOrderButton_Click(object? sender, EventArgs e)
    {
        using var createForm = _serviceProvider.GetRequiredService<OrderCreateForm>();
        createForm.ShowDialog(this);
    }

    private void OrdersButton_Click(object? sender, EventArgs e)
    {
        var listForm = _serviceProvider.GetRequiredService<OrderListForm>();
        listForm.Show(this);
    }

    // InAppNotifier.Notify can in principle be called from any thread, so marshal to the UI
    // thread before touching _notifications or any control.
    private void Notifier_Notified(object? sender, NotificationLogEntry entry)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Notifier_Notified(sender, entry));
            return;
        }

        _notifications.Add(MapToRow(entry));
    }

    private static NotificationRow MapToRow(NotificationLogEntry entry) => new()
    {
        Timestamp = entry.OccurredAtUtc.ToLocalTime(),
        OrderId = entry.Notification.OrderId,
        OldStatus = entry.Notification.OldStatus,
        NewStatus = entry.Notification.NewStatus
    };

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _notifier.Notified -= Notifier_Notified;
        base.OnFormClosed(e);
    }

    private sealed class NotificationRow
    {
        public DateTime Timestamp { get; set; }
        public int OrderId { get; set; }
        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
    }
}
