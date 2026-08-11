using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// Root IServiceProvider is injected solely to resolve+launch OrderDetailForm — never to
// resolve BLL/DAL services directly. All actual business operations go through
// OrderListPresenter's own per-action scope. See Story 1.3 Dev Notes.
public partial class OrderListForm : Form, IOrderListView
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OrderListPresenter _presenter;

    public OrderListForm(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _presenter = new OrderListPresenter(this, scopeFactory);
    }

    private async void OrderListForm_Load(object? sender, EventArgs e)
    {
        await _presenter.LoadOrdersAsync();
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        // Also disable viewButton — ShowDialog pumps a nested message loop, so a refresh
        // started just before opening OrderDetailForm could otherwise have its continuation
        // reset dataGridView.DataSource while the modal is open on top of it.
        refreshButton.Enabled = false;
        viewButton.Enabled = false;
        try
        {
            await _presenter.LoadOrdersAsync();
        }
        finally
        {
            refreshButton.Enabled = true;
            viewButton.Enabled = dataGridView.CurrentRow is not null;
        }
    }

    private void ViewButton_Click(object? sender, EventArgs e)
    {
        if (dataGridView.CurrentRow?.DataBoundItem is not OrderDto selected)
        {
            return;
        }

        // View-only — nothing on the list changes from viewing a detail, so no reload-on-close
        // like ProductListForm.OpenDetailFormAsync (which reloads because editing can change
        // the list). Story 3.3 adds status transitions to OrderDetailForm; that story is what
        // will need a reload here.
        using var detailForm = _serviceProvider.GetRequiredService<OrderDetailForm>();
        detailForm.Initialize(selected.Id);
        detailForm.ShowDialog(this);
    }

    private void DataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        viewButton.Enabled = dataGridView.CurrentRow is not null;
    }

    public void DisplayOrders(IReadOnlyList<OrderDto> orders)
    {
        dataGridView.DataSource = orders.ToList();

        // Id/CustomerId are redundant with CustomerName; Items is a nested collection that
        // AutoGenerateColumns can't render usefully (matches ProductListForm's Id-hiding
        // precedent, Story 1.5).
        foreach (var columnName in new[] { nameof(OrderDto.Id), nameof(OrderDto.CustomerId), nameof(OrderDto.Items) })
        {
            if (dataGridView.Columns[columnName] is { } column)
            {
                column.Visible = false;
            }
        }

        viewButton.Enabled = dataGridView.CurrentRow is not null;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Orders", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
