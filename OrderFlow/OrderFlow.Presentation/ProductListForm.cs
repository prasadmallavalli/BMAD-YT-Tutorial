using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// Root IServiceProvider is injected solely to resolve+launch other Transient Forms
// (ProductDetailForm) — never to resolve BLL/DAL services directly. All actual
// business operations go through ProductListPresenter's own per-action scope.
// See Story 1.3 Dev Notes.
public partial class ProductListForm : Form, IProductListView
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProductListPresenter _presenter;

    public ProductListForm(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _presenter = new ProductListPresenter(this, scopeFactory);
    }

    private async void ProductListForm_Load(object? sender, EventArgs e)
    {
        await _presenter.LoadProductsAsync();
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        refreshButton.Enabled = false;
        try
        {
            await _presenter.LoadProductsAsync();
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
            await OpenDetailFormAsync(productId: null);
        }
        finally
        {
            addButton.Enabled = true;
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (dataGridView.CurrentRow?.DataBoundItem is not ProductDto selected)
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

    private async Task OpenDetailFormAsync(int? productId)
    {
        using var detailForm = _serviceProvider.GetRequiredService<ProductDetailForm>();
        detailForm.Initialize(productId);

        if (detailForm.ShowDialog(this) == DialogResult.OK)
        {
            await _presenter.LoadProductsAsync();
        }
    }

    public void DisplayProducts(IReadOnlyList<ProductDto> products)
    {
        dataGridView.DataSource = products.ToList();

        if (dataGridView.Columns[nameof(ProductDto.Id)] is { } idColumn)
        {
            idColumn.Visible = false;
        }

        editButton.Enabled = dataGridView.CurrentRow is not null;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Products", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
