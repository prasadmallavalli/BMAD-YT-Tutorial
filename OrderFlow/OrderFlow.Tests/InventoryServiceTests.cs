using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task HasSufficientStockAsync_WithEnoughStock_ReturnsTrue()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        mockRepository.Setup(r => r.GetByProductIdAsync(1))
            .ReturnsAsync(new Inventory { ProductId = 1, StockQuantity = 10 });
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.HasSufficientStockAsync(1, 5);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task HasSufficientStockAsync_WithInsufficientStock_ReturnsFalse()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        mockRepository.Setup(r => r.GetByProductIdAsync(1))
            .ReturnsAsync(new Inventory { ProductId = 1, StockQuantity = 3 });
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.HasSufficientStockAsync(1, 5);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task HasSufficientStockAsync_WithNegativeRequestedQuantity_ReturnsFailure()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.HasSufficientStockAsync(1, -1);

        Assert.False(result.IsSuccess);
        mockRepository.Verify(r => r.GetByProductIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HasSufficientStockAsync_WithMissingProduct_ReturnsFailure()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        mockRepository.Setup(r => r.GetByProductIdAsync(It.IsAny<int>())).ReturnsAsync((Inventory?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.HasSufficientStockAsync(999, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product not found", result.Error);
    }

    [Fact]
    public async Task GetStockLevelAsync_WithExistingProduct_ReturnsQuantity()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        mockRepository.Setup(r => r.GetByProductIdAsync(1))
            .ReturnsAsync(new Inventory { ProductId = 1, StockQuantity = 42 });
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.GetStockLevelAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GetStockLevelAsync_WithMissingProduct_ReturnsFailure()
    {
        var mockRepository = new Mock<IInventoryRepository>();
        mockRepository.Setup(r => r.GetByProductIdAsync(It.IsAny<int>())).ReturnsAsync((Inventory?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Inventory).Returns(mockRepository.Object);

        var service = new InventoryService(mockUnitOfWork.Object);

        var result = await service.GetStockLevelAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product not found", result.Error);
    }
}
