using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// Leaf form — never launches another Form, so it only needs IServiceScopeFactory,
// unlike ProductListForm (which also needs IServiceProvider to launch this form).
// See Story 1.3 Dev Notes.
public partial class ProductDetailForm : Form, IProductDetailView
{
    private readonly ProductDetailPresenter _presenter;
    private int? _productId;

    public ProductDetailForm(IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _presenter = new ProductDetailPresenter(this, scopeFactory);
    }

    public void Initialize(int? productId)
    {
        _productId = productId;
        Text = productId.HasValue ? "Edit Product" : "Add Product";
    }

    private async void ProductDetailForm_Load(object? sender, EventArgs e)
    {
        if (_productId.HasValue)
        {
            await _presenter.LoadAsync(_productId.Value);
        }
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        saveButton.Enabled = false;
        try
        {
            var dto = new ProductDto
            {
                Name = nameTextBox.Text,
                SKU = skuTextBox.Text,
                UnitPrice = unitPriceNumericUpDown.Value,
                StockQuantity = (int)stockQuantityNumericUpDown.Value,
            };

            if (await _presenter.SaveAsync(_productId, dto))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        finally
        {
            saveButton.Enabled = true;
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    public void ShowProduct(ProductDto product)
    {
        nameTextBox.Text = product.Name;
        skuTextBox.Text = product.SKU;
        unitPriceNumericUpDown.Value = Math.Clamp(
            product.UnitPrice, unitPriceNumericUpDown.Minimum, unitPriceNumericUpDown.Maximum);
        stockQuantityNumericUpDown.Value = Math.Clamp(
            (decimal)product.StockQuantity, stockQuantityNumericUpDown.Minimum, stockQuantityNumericUpDown.Maximum);
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Product", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
