using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderFlow.BLL;

namespace OrderFlow.Presentation.Tests;

public class CustomerListPresenterTests
{
    private static (Mock<IServiceScopeFactory> scopeFactory, Mock<ICustomerService> service) CreateMockScope() =>
        MockScopeHelper.CreateMockScope<ICustomerService>();

    [Fact]
    public async Task LoadCustomersAsync_OnSuccess_DisplaysCustomers()
    {
        var (scopeFactory, service) = CreateMockScope();
        var customers = new List<CustomerDto>
        {
            new() { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" },
        };
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<IReadOnlyList<CustomerDto>>.Success(customers));

        var mockView = new Mock<ICustomerListView>();
        var presenter = new CustomerListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadCustomersAsync();

        mockView.Verify(v => v.DisplayCustomers(customers), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadCustomersAsync_OnFailure_ShowsError()
    {
        var (scopeFactory, service) = CreateMockScope();
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<IReadOnlyList<CustomerDto>>.Failure("boom"));

        var mockView = new Mock<ICustomerListView>();
        var presenter = new CustomerListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadCustomersAsync();

        mockView.Verify(v => v.ShowError("boom"), Times.Once);
        mockView.Verify(v => v.DisplayCustomers(It.IsAny<IReadOnlyList<CustomerDto>>()), Times.Never);
    }
}
