using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface ICustomerListView
{
    void DisplayCustomers(IReadOnlyList<CustomerDto> customers);
    void ShowError(string message);
}
