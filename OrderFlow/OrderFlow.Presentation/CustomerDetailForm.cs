using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BLL;

namespace OrderFlow.Presentation;

// Leaf form — never launches another Form, so it only needs IServiceScopeFactory,
// unlike CustomerListForm (which also needs IServiceProvider to launch this form).
// See Story 1.3 Dev Notes.
public partial class CustomerDetailForm : Form, ICustomerDetailView
{
    private readonly CustomerDetailPresenter _presenter;
    private int? _customerId;

    public CustomerDetailForm(IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _presenter = new CustomerDetailPresenter(this, scopeFactory);
    }

    public void Initialize(int? customerId)
    {
        _customerId = customerId;
        Text = customerId.HasValue ? "Edit Customer" : "Add Customer";
    }

    private async void CustomerDetailForm_Load(object? sender, EventArgs e)
    {
        if (_customerId.HasValue)
        {
            await _presenter.LoadAsync(_customerId.Value);
        }
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        saveButton.Enabled = false;
        try
        {
            var dto = new CustomerDto
            {
                Name = nameTextBox.Text,
                Email = emailTextBox.Text,
                Phone = phoneTextBox.Text,
            };

            if (await _presenter.SaveAsync(_customerId, dto))
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

    public void ShowCustomer(CustomerDto customer)
    {
        nameTextBox.Text = customer.Name;
        emailTextBox.Text = customer.Email;
        phoneTextBox.Text = customer.Phone;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, "Customer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
