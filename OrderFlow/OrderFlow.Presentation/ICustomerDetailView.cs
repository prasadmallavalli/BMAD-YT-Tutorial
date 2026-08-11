using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface ICustomerDetailView
{
    void ShowCustomer(CustomerDto customer);
    void ShowError(string message);
}
