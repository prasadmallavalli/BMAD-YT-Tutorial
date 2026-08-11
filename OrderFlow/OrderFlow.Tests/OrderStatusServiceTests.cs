using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class OrderStatusServiceTests
{
    [Fact]
    public async Task TransitionTo_FromUnspecifiedToConfirmed_ReturnsSuccessAndFiresExactlyOneNotification()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Unspecified };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Confirmed);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, result.Value);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OrderId == order.Id && x.OldStatus == OrderStatus.Unspecified && x.NewStatus == OrderStatus.Confirmed)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_WithMissingOrder_ReturnsFailureAndDoesNotNotify()
    {
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(999, OrderStatus.Confirmed);

        Assert.False(result.IsSuccess);
        Assert.Equal("Order not found", result.Error);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_WithNoMatchingTableEntry_ReturnsFailureAndDoesNotNotify()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Confirmed };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Confirmed);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_WithOrderTypeNotInTable_ReturnsFailureAndDoesNotNotify()
    {
        // OrderType.Unspecified has no partition in AllowedTransitions at all — distinct from
        // TransitionTo_WithNoMatchingTableEntry, which covers a known OrderType with no entry
        // for the order's current status.
        var order = new Order { Id = 1, OrderType = OrderType.Unspecified, Status = OrderStatus.Unspecified };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Confirmed);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Unspecified, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_OnConcurrencyConflict_ReturnsFriendlyFailureAndDoesNotNotify()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Unspecified };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new ConcurrencyConflictException(new Exception()));
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Confirmed);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConcurrencyConflictException.DefaultMessage, result.Error);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_Standard_ConfirmedToProcessing_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Confirmed };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Processing);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Confirmed && x.NewStatus == OrderStatus.Processing)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Standard_ProcessingToCancelled_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Processing };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Processing && x.NewStatus == OrderStatus.Cancelled)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Standard_ShippedToCancelled_ReturnsFailureAndDoesNotNotify()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Shipped };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_Rush_ConfirmedToProcessing_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Rush, Status = OrderStatus.Confirmed };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Processing);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Confirmed && x.NewStatus == OrderStatus.Processing)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Rush_ProcessingToCancelled_ReturnsFailureAndDoesNotNotify()
    {
        // Rush-specific restriction (AC #4): unlike Standard, Rush's Processing state has no
        // Cancelled entry at all — once a Rush order starts Processing, it's committed.
        var order = new Order { Id = 1, OrderType = OrderType.Rush, Status = OrderStatus.Processing };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public async Task TransitionTo_Rush_ConfirmedToCancelled_Succeeds()
    {
        // Confirms Rush's Cancelled path still works from Confirmed — only Processing->Cancelled
        // is blocked (see TransitionTo_Rush_ProcessingToCancelled_ReturnsFailureAndDoesNotNotify).
        var order = new Order { Id = 1, OrderType = OrderType.Rush, Status = OrderStatus.Confirmed };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Confirmed && x.NewStatus == OrderStatus.Cancelled)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Standard_ConfirmedToCancelled_Succeeds()
    {
        // Closes the other half of Standard's "Cancelled reachable from Confirmed or
        // Processing" rule — TransitionTo_Standard_ProcessingToCancelled_Succeeds covers the
        // Processing half.
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Confirmed };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Confirmed && x.NewStatus == OrderStatus.Cancelled)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Standard_ProcessingToShipped_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Processing };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Shipped);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Processing && x.NewStatus == OrderStatus.Shipped)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Standard_ShippedToDelivered_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Shipped };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Delivered);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Shipped && x.NewStatus == OrderStatus.Delivered)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Rush_ProcessingToShipped_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Rush, Status = OrderStatus.Processing };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Shipped);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Processing && x.NewStatus == OrderStatus.Shipped)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_Rush_ShippedToDelivered_Succeeds()
    {
        var order = new Order { Id = 1, OrderType = OrderType.Rush, Status = OrderStatus.Shipped };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Delivered);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        mockNotifier.Verify(
            n => n.Notify(It.Is<OrderStatusChangedNotification>(x =>
                x.OldStatus == OrderStatus.Shipped && x.NewStatus == OrderStatus.Delivered)),
            Times.Once);
    }

    [Fact]
    public async Task TransitionTo_FromDelivered_ReturnsFailureAndDoesNotNotify()
    {
        // Delivered is terminal — no entry in either OrderType's partition, proven here rather
        // than only asserted in a comment.
        var order = new Order { Id = 1, OrderType = OrderType.Standard, Status = OrderStatus.Delivered };
        var mockOrderRepository = new Mock<IOrderRepository>();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);
        var mockNotifier = new Mock<INotifier>();

        var service = new OrderStatusService(mockUnitOfWork.Object, mockNotifier.Object);

        var result = await service.TransitionTo(1, OrderStatus.Cancelled);

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never);
    }

    [Fact]
    public void GetAllowedNextStatuses_ForStandardConfirmed_ReturnsProcessingAndCancelled()
    {
        var service = new OrderStatusService(Mock.Of<IUnitOfWork>(), Mock.Of<INotifier>());

        var allowed = service.GetAllowedNextStatuses(OrderType.Standard, OrderStatus.Confirmed);

        Assert.Equal([OrderStatus.Processing, OrderStatus.Cancelled], allowed);
    }

    [Fact]
    public void GetAllowedNextStatuses_ForRushProcessing_ReturnsShippedOnly()
    {
        // Rush-specific restriction (Story 3.1): unlike Standard, Rush's Processing state has
        // no Cancelled entry at all.
        var service = new OrderStatusService(Mock.Of<IUnitOfWork>(), Mock.Of<INotifier>());

        var allowed = service.GetAllowedNextStatuses(OrderType.Rush, OrderStatus.Processing);

        Assert.Equal([OrderStatus.Shipped], allowed);
    }

    [Fact]
    public void GetAllowedNextStatuses_ForTerminalStatus_ReturnsEmpty()
    {
        var service = new OrderStatusService(Mock.Of<IUnitOfWork>(), Mock.Of<INotifier>());

        var allowed = service.GetAllowedNextStatuses(OrderType.Standard, OrderStatus.Delivered);

        Assert.Empty(allowed);
    }

    [Fact]
    public void GetAllowedNextStatuses_ForUnknownOrderType_ReturnsEmpty()
    {
        var service = new OrderStatusService(Mock.Of<IUnitOfWork>(), Mock.Of<INotifier>());

        var allowed = service.GetAllowedNextStatuses(OrderType.Unspecified, OrderStatus.Unspecified);

        Assert.Empty(allowed);
    }
}
