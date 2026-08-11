using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation;

public interface IOrderDetailView
{
    void ShowOrder(OrderDto order);
    void DisplayAvailableTransitions(IReadOnlyList<OrderStatus> allowedStatuses);
    void ShowError(string message);
}
