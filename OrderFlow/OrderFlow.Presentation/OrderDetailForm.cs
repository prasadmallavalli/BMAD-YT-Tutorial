using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

// Leaf form — never launches another Form, so it only needs IServiceScopeFactory,
// matching ProductDetailForm/OrderCreateForm's precedent (Story 1.3/2.5).
public partial class OrderDetailForm : Form, IOrderDetailView
{
    private readonly OrderDetailPresenter _presenter;
    private int _orderId;

    public OrderDetailForm(IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _presenter = new OrderDetailPresenter(this, scopeFactory);
    }

    // Non-nullable, unlike ProductDetailForm.Initialize(int? productId) — this form only ever
    // views an existing Order, there is no "create" case here.
    public void Initialize(int orderId)
    {
        _orderId = orderId;
    }

    private async void OrderDetailForm_Load(object? sender, EventArgs e)
    {
        await _presenter.LoadAsync(_orderId);
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private async void TransitionButton_Click(object? sender, EventArgs e)
    {
        if (statusComboBox.SelectedItem is not OrderStatus newStatus)
        {
            return;
        }

        statusComboBox.Enabled = false;
        transitionButton.Enabled = false;
        var transitioned = false;
        try
        {
            // Form-orchestrated reload, not chained inside the Presenter (AD-5) — mirrors
            // OrderListForm/ProductListForm reloading after a successful child operation.
            transitioned = await _presenter.TransitionToAsync(_orderId, newStatus);
            if (transitioned)
            {
                await _presenter.LoadAsync(_orderId);
            }
        }
        finally
        {
            // Once the transition succeeds, LoadAsync's DisplayAvailableTransitions becomes
            // the sole authority on both controls' enabled state — whether the reload then
            // succeeds (fresh allowed-list) or fails (ShowError, nothing further to display),
            // don't re-enable from the stale pre-transition list here. Only restore the prior
            // state when the transition itself was rejected (AC #3) and nothing changed.
            if (!transitioned)
            {
                statusComboBox.Enabled = statusComboBox.Items.Count > 0;
                transitionButton.Enabled = statusComboBox.Items.Count > 0;
            }
        }
    }

    public void ShowOrder(OrderDto order)
    {
        customerValueLabel.Text = order.CustomerName;
        orderTypeValueLabel.Text = order.OrderType.ToString();
        statusValueLabel.Text = order.Status.ToString();
        totalValueLabel.Text = order.Total.ToString("C");

        itemsDataGridView.DataSource = order.Items.ToList();

        if (itemsDataGridView.Columns[nameof(OrderItemDto.ProductId)] is { } productIdColumn)
        {
            productIdColumn.Visible = false;
        }
    }

    public void DisplayAvailableTransitions(IReadOnlyList<OrderStatus> allowedStatuses)
    {
        statusComboBox.DataSource = allowedStatuses.ToList();
        statusComboBox.Enabled = allowedStatuses.Count > 0;
        transitionButton.Enabled = allowedStatuses.Count > 0;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Order Detail", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
