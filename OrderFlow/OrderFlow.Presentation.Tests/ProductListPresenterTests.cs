using Moq;
using OrderFlow.BLL;

namespace OrderFlow.Presentation.Tests;

public class ProductListPresenterTests
{
    [Fact]
    public async Task LoadProductsAsync_OnSuccess_DisplaysProducts()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        var products = new List<ProductDto>
        {
            new() { Id = 1, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 },
        };
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<IReadOnlyList<ProductDto>>.Success(products));

        var mockView = new Mock<IProductListView>();
        var presenter = new ProductListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadProductsAsync();

        mockView.Verify(v => v.DisplayProducts(products), Times.Once);
        mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadProductsAsync_OnFailure_ShowsError()
    {
        var (scopeFactory, service) = MockScopeHelper.CreateMockScope<IProductService>();
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<IReadOnlyList<ProductDto>>.Failure("boom"));

        var mockView = new Mock<IProductListView>();
        var presenter = new ProductListPresenter(mockView.Object, scopeFactory.Object);

        await presenter.LoadProductsAsync();

        mockView.Verify(v => v.ShowError("boom"), Times.Once);
        mockView.Verify(v => v.DisplayProducts(It.IsAny<IReadOnlyList<ProductDto>>()), Times.Never);
    }
}
