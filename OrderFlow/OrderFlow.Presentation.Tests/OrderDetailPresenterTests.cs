using Moq;
using OrderFlow.BLL;
using OrderFlow.Domain;

namespace OrderFlow.Presentation.Tests;

public class OrderDetailPresenterTests
{
    [Fact]
    public async Task LoadAsync_WithExistingOrder_ShowsOrderAndDisplaysAvailableTransitions()
    {
        var (scopeFactory, orderService, orderStatusService) =
            MockScopeHelper.CreateMockScope<IOrderService, IOrderStatusService>();
        var order = new OrderDto
        {
            Id = 1, CustomerId = 1, CustomerName = "Ada Lovelace",
            OrderType = OrderType.Standard, Status = OrderStatus.Confirmed, Total = 19.98m
        };
        orderService.Setup(s => s.GetAsync(1)).ReturnsAsync(Result<OrderDto>.Success(order));
        var allowedStatuses = new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled };
        orderStatusService.Setup(s => s.GetAllowedNextStatuses(OrderType.Standard, OrderStatus.Confirmed))
            .Returns(allowedStatuses);

        var mockView = new Mock<IOrderDetailView>();
        var presenter = new OrderDetailPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadAsync(1);

        mockView.Verify(v => v.ShowOrder(order), Times.Once);
        mockView.Verify(v => v.DisplayAvailableTransitions(allowedStatuses), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_WithTerminalStatusOrder_DisplaysEmptyAllowedTransitions()
    {
        var (scopeFactory, orderService, orderStatusService) =
            MockScopeHelper.CreateMockScope<IOrderService, IOrderStatusService>();
        var order = new OrderDto
        {
            Id = 1, CustomerId = 1, CustomerName = "Ada Lovelace",
            OrderType = OrderType.Standard, Status = OrderStatus.Delivered, Total = 19.98m
        };
        orderService.Setup(s => s.GetAsync(1)).ReturnsAsync(Result<OrderDto>.Success(order));
        orderStatusService.Setup(s => s.GetAllowedNextStatuses(OrderType.Standard, OrderStatus.Delivered))
            .Returns(Array.Empty<OrderStatus>());

        var mockView = new Mock<IOrderDetailView>();
        var presenter = new OrderDetailPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadAsync(1);

        mockView.Verify(v => v.ShowOrder(order), Times.Once);
        mockView.Verify(
            v => v.DisplayAvailableTransitions(It.Is<IReadOnlyList<OrderStatus>>(list => list.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithMissingOrder_ShowsErrorAndDoesNotDisplayTransitions()
    {
        var (scopeFactory, orderService, _) =
            MockScopeHelper.CreateMockScope<IOrderService, IOrderStatusService>();
        orderService.Setup(s => s.GetAsync(999)).ReturnsAsync(Result<OrderDto>.Failure("Order not found"));

        var mockView = new Mock<IOrderDetailView>();
        var presenter = new OrderDetailPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadAsync(999);

        mockView.Verify(v => v.ShowError("Order not found"), Times.Once);
        mockView.Verify(v => v.ShowOrder(It.IsAny<OrderDto>()), Times.Never);
        mockView.Verify(v => v.DisplayAvailableTransitions(It.IsAny<IReadOnlyList<OrderStatus>>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToAsync_OnSuccess_ReturnsTrue()
    {
        var (scopeFactory, orderStatusService) = MockScopeHelper.CreateMockScope<IOrderStatusService>();
        orderStatusService.Setup(s => s.TransitionTo(1, OrderStatus.Processing))
            .ReturnsAsync(Result<OrderStatus>.Success(OrderStatus.Processing));

        var mockView = new Mock<IOrderDetailView>();
        var presenter = new OrderDetailPresenter(mockView.Object, scopeFactory.Object);

        var succeeded = await presenter.TransitionToAsync(1, OrderStatus.Processing);

        Assert.True(succeeded);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransitionToAsync_OnFailure_ShowsErrorAndReturnsFalse()
    {
        // Simulates AC #3's stale-UI-state rejection.
        var (scopeFactory, orderStatusService) = MockScopeHelper.CreateMockScope<IOrderStatusService>();
        orderStatusService.Setup(s => s.TransitionTo(1, OrderStatus.Cancelled))
            .ReturnsAsync(Result<OrderStatus>.Failure("Cannot transition Order 1 from Processing to Cancelled"));

        var mockView = new Mock<IOrderDetailView>();
        var presenter = new OrderDetailPresenter(mockView.Object, scopeFactory.Object);

        var succeeded = await presenter.TransitionToAsync(1, OrderStatus.Cancelled);

        Assert.False(succeeded);
        mockView.Verify(v => v.ShowError("Cannot transition Order 1 from Processing to Cancelled"), Times.Once);
    }
}
