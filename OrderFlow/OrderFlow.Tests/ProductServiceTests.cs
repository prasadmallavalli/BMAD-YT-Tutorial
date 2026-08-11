using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidProduct_ReturnsSuccessAndPersists()
    {
        var mockRepository = new Mock<IProductRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = new ProductService(mockUnitOfWork.Object);
        var dto = new ProductDto { Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, StockQuantity = 10 };

        var result = await service.CreateAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(dto.Name, result.Value!.Name);
        Assert.Equal(dto.StockQuantity, result.Value.StockQuantity);
        mockRepository.Verify(r => r.AddAsync(It.Is<Product>(p =>
            p.Name == dto.Name && p.SKU == dto.SKU && p.Inventory!.StockQuantity == dto.StockQuantity)), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("", "SKU-1", 1.0, 0)]
    [InlineData("Widget", "", 1.0, 0)]
    [InlineData("Widget", "SKU-1", 0, 0)]
    [InlineData("Widget", "SKU-1", -1, 0)]
    [InlineData("Widget", "SKU-1", 1.0, -1)]
    public async Task CreateAsync_WithInvalidProduct_ReturnsFailureAndDoesNotPersist(string name, string sku, decimal unitPrice, int stockQuantity)
    {
        var mockRepository = new Mock<IProductRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);

        var service = new ProductService(mockUnitOfWork.Object);
        var dto = new ProductDto { Name = name, SKU = sku, UnitPrice = unitPrice, StockQuantity = stockQuantity };

        var result = await service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingId_ReturnsNotFoundEvenWhenDtoIsInvalid()
    {
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);

        var service = new ProductService(mockUnitOfWork.Object);
        var invalidDto = new ProductDto { Name = "", SKU = "" };

        var result = await service.UpdateAsync(999, invalidDto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product not found", result.Error);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingIdAndValidDto_MutatesTrackedEntityAndPersists()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Old Name",
            SKU = "OLD-1",
            UnitPrice = 1.0m,
            Inventory = new Inventory { StockQuantity = 5 },
        };
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = new ProductService(mockUnitOfWork.Object);
        var dto = new ProductDto { Name = "New Name", SKU = "NEW-1", UnitPrice = 2.0m, StockQuantity = 20 };

        var result = await service.UpdateAsync(1, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", product.Name);
        Assert.Equal(20, product.Inventory!.StockQuantity);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OnConcurrencyConflict_ReturnsFriendlyFailure()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Old Name",
            SKU = "OLD-1",
            UnitPrice = 1.0m,
            Inventory = new Inventory { StockQuantity = 5 },
        };
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new ConcurrencyConflictException(new Exception()));

        var service = new ProductService(mockUnitOfWork.Object);
        var dto = new ProductDto { Name = "New Name", SKU = "NEW-1", UnitPrice = 2.0m, StockQuantity = 20 };

        var result = await service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConcurrencyConflictException.DefaultMessage, result.Error);
    }

    [Fact]
    public async Task GetAsync_WithExistingId_ReturnsSuccess()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Widget",
            SKU = "WID-001",
            UnitPrice = 9.99m,
            Inventory = new Inventory { StockQuantity = 10 },
        };
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);

        var service = new ProductService(mockUnitOfWork.Object);

        var result = await service.GetAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Name, result.Value!.Name);
        Assert.Equal(product.Inventory.StockQuantity, result.Value.StockQuantity);
    }

    [Fact]
    public async Task GetAsync_WithMissingId_ReturnsFailure()
    {
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);

        var service = new ProductService(mockUnitOfWork.Object);

        var result = await service.GetAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product not found", result.Error);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProductsAsDtos()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", SKU = "WID-001", UnitPrice = 9.99m, Inventory = new Inventory { StockQuantity = 10 } },
            new() { Id = 2, Name = "Gadget", SKU = "GAD-001", UnitPrice = 19.99m, Inventory = new Inventory { StockQuantity = 3 } },
        };
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Products).Returns(mockRepository.Object);

        var service = new ProductService(mockUnitOfWork.Object);

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }
}
