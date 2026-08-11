using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface IOrderListView
{
    void DisplayOrders(IReadOnlyList<OrderDto> orders);
    void ShowError(string message);
}
