using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderFlow.BLL;

namespace OrderFlow.Presentation.Tests;

public class CustomerDetailPresenterTests
{
    private static (Mock<IServiceScopeFactory> scopeFactory, Mock<ICustomerService> service) CreateMockScope() =>
        MockScopeHelper.CreateMockScope<ICustomerService>();

    [Fact]
    public async Task LoadAsync_WithExistingId_ShowsCustomerAndReturnsTrue()
    {
        var (scopeFactory, service) = CreateMockScope();
        var customer = new CustomerDto { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" };
        service.Setup(s => s.GetAsync(1)).ReturnsAsync(Result<CustomerDto>.Success(customer));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var loaded = await presenter.LoadAsync(1);

        Assert.True(loaded);
        mockView.Verify(v => v.ShowCustomer(customer), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_WithMissingId_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = CreateMockScope();
        service.Setup(s => s.GetAsync(999)).ReturnsAsync(Result<CustomerDto>.Failure("Customer not found"));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var loaded = await presenter.LoadAsync(999);

        Assert.False(loaded);
        mockView.Verify(v => v.ShowError("Customer not found"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNullCustomerId_CallsCreateAsync()
    {
        var (scopeFactory, service) = CreateMockScope();
        var dto = new CustomerDto { Name = "Ada Lovelace", Email = "ada@example.com" };
        service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(Result<CustomerDto>.Success(dto));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(null, dto);

        Assert.True(saved);
        service.Verify(s => s.CreateAsync(dto), Times.Once);
        service.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<CustomerDto>()), Times.Never);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithCustomerId_CallsUpdateAsync()
    {
        var (scopeFactory, service) = CreateMockScope();
        var dto = new CustomerDto { Name = "Ada Lovelace", Email = "ada@example.com" };
        service.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(Result<CustomerDto>.Success(dto));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(1, dto);

        Assert.True(saved);
        service.Verify(s => s.UpdateAsync(1, dto), Times.Once);
        service.Verify(s => s.CreateAsync(It.IsAny<CustomerDto>()), Times.Never);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_OnCreateFailure_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = CreateMockScope();
        var dto = new CustomerDto { Name = "", Email = "" };
        service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(Result<CustomerDto>.Failure("Name is required"));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(null, dto);

        Assert.False(saved);
        mockView.Verify(v => v.ShowError("Name is required"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_OnUpdateFailure_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = CreateMockScope();
        var dto = new CustomerDto { Name = "", Email = "" };
        service.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(Result<CustomerDto>.Failure("Name is required"));

        var mockView = new Mock<ICustomerDetailView>();
        var presenter = new CustomerDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(1, dto);

        Assert.False(saved);
        mockView.Verify(v => v.ShowError("Name is required"), Times.Once);
    }
}
