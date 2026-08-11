using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// Root IServiceProvider is injected solely to resolve+launch other Transient Forms
// (CustomerDetailForm) — never to resolve BLL/DAL services directly. All actual
// business operations go through CustomerListPresenter's own per-action scope.
// See Story 1.3 Dev Notes.
public partial class CustomerListForm : Form, ICustomerListView
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CustomerListPresenter _presenter;

    public CustomerListForm(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _presenter = new CustomerListPresenter(this, scopeFactory);
    }

    private async void CustomerListForm_Load(object? sender, EventArgs e)
    {
        await _presenter.LoadCustomersAsync();
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        refreshButton.Enabled = false;
        try
        {
            await _presenter.LoadCustomersAsync();
        }
        finally
        {
            refreshButton.Enabled = true;
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        addButton.Enabled = false;
        try
        {
            await OpenDetailFormAsync(customerId: null);
        }
        finally
        {
            addButton.Enabled = true;
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (dataGridView.CurrentRow?.DataBoundItem is not CustomerDto selected)
        {
            return;
        }

        editButton.Enabled = false;
        try
        {
            await OpenDetailFormAsync(selected.Id);
        }
        finally
        {
            editButton.Enabled = dataGridView.CurrentRow is not null;
        }
    }

    private void DataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        editButton.Enabled = dataGridView.CurrentRow is not null;
    }

    private async Task OpenDetailFormAsync(int? customerId)
    {
        using var detailForm = _serviceProvider.GetRequiredService<CustomerDetailForm>();
        detailForm.Initialize(customerId);

        if (detailForm.ShowDialog(this) == DialogResult.OK)
        {
            await _presenter.LoadCustomersAsync();
        }
    }

    public void DisplayCustomers(IReadOnlyList<CustomerDto> customers)
    {
        dataGridView.DataSource = customers.ToList();

        if (dataGridView.Columns[nameof(CustomerDto.Id)] is { } idColumn)
        {
            idColumn.Visible = false;
        }

        editButton.Enabled = dataGridView.CurrentRow is not null;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Customers", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
