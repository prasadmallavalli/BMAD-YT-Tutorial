using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace OrderFlow.Presentation.Tests;

// Shared helper for mocking the IServiceScopeFactory -> IServiceScope -> IServiceProvider
// chain Presenters use. Mocks CreateScope() (the real interface method), not
// CreateAsyncScope() (an extension method that wraps it) — see Story 1.3 Dev Notes.
// Generalized in Story 1.5 (was hardcoded to ICustomerService) so every UI story's
// presenter tests reuse this instead of each adding their own copy.
internal static class MockScopeHelper
{
    public static (Mock<IServiceScopeFactory> scopeFactory, Mock<TService> service) CreateMockScope<TService>()
        where TService : class
    {
        var mockService = new Mock<TService>();
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(p => p.GetService(typeof(TService))).Returns(mockService.Object);
        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockProvider.Object);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        return (mockScopeFactory, mockService);
    }

    // Two-service overload — for a Presenter method that resolves multiple BLL services from
    // the same scope in one business operation (AD-3), e.g. OrderDetailPresenter.LoadAsync
    // (Story 3.3) resolving both IOrderService and IOrderStatusService.
    public static (Mock<IServiceScopeFactory> scopeFactory, Mock<TService1> service1, Mock<TService2> service2)
        CreateMockScope<TService1, TService2>()
        where TService1 : class
        where TService2 : class
    {
        var mockService1 = new Mock<TService1>();
        var mockService2 = new Mock<TService2>();
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(p => p.GetService(typeof(TService1))).Returns(mockService1.Object);
        mockProvider.Setup(p => p.GetService(typeof(TService2))).Returns(mockService2.Object);
        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockProvider.Object);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        return (mockScopeFactory, mockService1, mockService2);
    }
}
