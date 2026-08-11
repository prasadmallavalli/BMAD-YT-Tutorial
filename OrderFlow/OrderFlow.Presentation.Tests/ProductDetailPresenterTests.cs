using Moq;
using OrderFlow.BLL;

namespace OrderFlow.Presentation.Tests;

public class ProductDetailPresenterTests
{
    [Fact]
    public async Task LoadAsync_WithExistingId_ShowsProductAndReturnsTrue()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var product = new ProductDto { Id = 1, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 };
        service.Setup(s => s.GetAsync(1)).ReturnsAsync(Result<ProductDto>.Success(product));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var loaded = await presenter.LoadAsync(1);

        Assert.True(loaded);
        mockView.Verify(v => v.ShowProduct(product), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_WithMissingId_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        service.Setup(s => s.GetAsync(999)).ReturnsAsync(Result<ProductDto>.Failure("Product not found"));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var loaded = await presenter.LoadAsync(999);

        Assert.False(loaded);
        mockView.Verify(v => v.ShowError("Product not found"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNullProductId_CallsCreateAsync()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var dto = new ProductDto { Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 };
        service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(Result<ProductDto>.Success(dto));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(null, dto);

        Assert.True(saved);
        service.Verify(s => s.CreateAsync(dto), Times.Once);
        service.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<ProductDto>()), Times.Never);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithProductId_CallsUpdateAsync()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var dto = new ProductDto { Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 };
        service.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(Result<ProductDto>.Success(dto));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(1, dto);

        Assert.True(saved);
        service.Verify(s => s.UpdateAsync(1, dto), Times.Once);
        service.Verify(s => s.CreateAsync(It.IsAny<ProductDto>()), Times.Never);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_OnCreateFailure_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var dto = new ProductDto { Name = "", SKU = "" };
        service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(Result<ProductDto>.Failure("Name is required"));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(null, dto);

        Assert.False(saved);
        mockView.Verify(v => v.ShowError("Name is required"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_OnUpdateFailure_ShowsErrorAndReturnsFalse()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var dto = new ProductDto { Name = "", SKU = "" };
        service.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(Result<ProductDto>.Failure("Name is required"));

        var mockView = new Mock<IProductDetailView>();
        var presenter = new ProductDetailPresenter(mockView.Object, scopeFactory.Object);

        var saved = await presenter.SaveAsync(1, dto);

        Assert.False(saved);
        mockView.Verify(v => v.ShowError("Name is required"), Times.Once);
    }
}
