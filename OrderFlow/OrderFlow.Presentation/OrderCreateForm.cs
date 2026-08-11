using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

// Leaf form — never launches another Form, so it only needs IServiceScopeFactory,
// matching ProductDetailForm's Story 1.3 precedent (not IServiceProvider).
public partial class OrderCreateForm : Form, IOrderCreateView
{
    private readonly OrderCreatePresenter _presenter;
    private readonly BindingList<OrderLineItemRow> _lineItems = [];
    private IReadOnlyList<ProductDto> _products = [];

    public OrderCreateForm(IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _presenter = new OrderCreatePresenter(this, scopeFactory);

        lineItemsDataGridView.DataSource = _lineItems;

        // OrderType options are static (Domain enum), not server-loaded — Unspecified is
        // never a user-selectable choice (AD-4 forbids the processor from setting it either).
        orderTypeComboBox.DataSource = Enum.GetValues<OrderType>()
            .Where(t => t != OrderType.Unspecified)
            .ToList();
    }

    private async void OrderCreateForm_Load(object? sender, EventArgs e)
    {
        await _presenter.LoadCustomersAsync();
        await _presenter.LoadProductsAsync();
    }

    private void AddItemButton_Click(object? sender, EventArgs e)
    {
        if (productComboBox.SelectedValue is not int productId)
        {
            return;
        }

        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product is null)
        {
            return;
        }

        _lineItems.Add(new OrderLineItemRow
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = (int)quantityNumericUpDown.Value,
            UnitPriceAtOrder = product.UnitPrice
        });
    }

    private void RemoveItemButton_Click(object? sender, EventArgs e)
    {
        if (lineItemsDataGridView.CurrentRow?.DataBoundItem is OrderLineItemRow selected)
        {
            _lineItems.Remove(selected);
        }
    }

    private async void ConfirmButton_Click(object? sender, EventArgs e)
    {
        confirmButton.Enabled = false;
        try
        {
            if (customerComboBox.SelectedValue is not int customerId)
            {
                ShowError("Please select a customer.");
                return;
            }

            if (orderTypeComboBox.SelectedItem is not OrderType orderType)
            {
                ShowError("Please select an order type.");
                return;
            }

            if (_lineItems.Count == 0)
            {
                ShowError("Add at least one line item.");
                return;
            }

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                OrderType = orderType,
                Items = _lineItems.Select(row => new OrderItemDto
                {
                    ProductId = row.ProductId,
                    Quantity = row.Quantity,
                    UnitPriceAtOrder = row.UnitPriceAtOrder
                }).ToList()
            };

            if (await _presenter.ConfirmAsync(request))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        finally
        {
            confirmButton.Enabled = true;
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    public void DisplayCustomers(IReadOnlyList<CustomerDto> customers)
    {
        customerComboBox.DataSource = customers.ToList();
        customerComboBox.DisplayMember = nameof(CustomerDto.Name);
        customerComboBox.ValueMember = nameof(CustomerDto.Id);
    }

    public void DisplayProducts(IReadOnlyList<ProductDto> products)
    {
        _products = products;
        productComboBox.DataSource = products.ToList();
        productComboBox.DisplayMember = nameof(ProductDto.Name);
        productComboBox.ValueMember = nameof(ProductDto.Id);
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "New Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
