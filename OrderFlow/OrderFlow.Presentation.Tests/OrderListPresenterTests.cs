using Moq;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation.Tests;

public class OrderListPresenterTests
{
    [Fact]
    public async Task LoadOrdersAsync_OnSuccess_DisplaysOrders()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IOrderService>();
        var orders = new List<OrderDto>
        {
            new()
            {
                Id = 1, CustomerId = 1, CustomerName = "Ada Lovelace",
                OrderType = OrderType.Standard, Status = OrderStatus.Confirmed, Total = 19.98m
            }
        };
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<OrderDto>>.Success(orders));

        var mockView = new Mock<IOrderListView>();
        var presenter = new OrderListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadOrdersAsync();

        mockView.Verify(v => v.DisplayOrders(orders), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadOrdersAsync_OnFailure_ShowsError()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IOrderService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IReadOnlyList<OrderDto>>.Failure("boom"));

        var mockView = new Mock<IOrderListView>();
        var presenter = new OrderListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadOrdersAsync();

        mockView.Verify(v => v.ShowError("boom"), Times.Once);
        mockView.Verify(v => v.DisplayOrders(It.IsAny<IReadOnlyList<OrderDto>>()), Times.Never);
    }
}
