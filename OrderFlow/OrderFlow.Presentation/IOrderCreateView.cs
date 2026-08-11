using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface IOrderCreateView
{
    void DisplayCustomers(IReadOnlyList<CustomerDto> customers);
    void DisplayProducts(IReadOnlyList<ProductDto> products);
    void ShowError(string message);
}
