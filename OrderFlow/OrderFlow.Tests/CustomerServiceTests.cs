using Moq;
using OrderFlow.BLL;
using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidCustomer_ReturnsSuccessAndPersists()
    {
        var mockRepository = new Mock<ICustomerRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = new CustomerService(mockUnitOfWork.Object);
        var dto = new CustomerDto { Name = "Ada Lovelace", Email = "ada@example.com", Phone = "555-0100" };

        var result = await service.CreateAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(dto.Name, result.Value!.Name);
        Assert.Equal(dto.Email, result.Value.Email);
        mockRepository.Verify(r => r.AddAsync(It.Is<Customer>(c => c.Name == dto.Name && c.Email == dto.Email)), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("", "ada@example.com")]
    [InlineData("Ada Lovelace", "")]
    public async Task CreateAsync_WithMissingRequiredField_ReturnsFailureAndDoesNotPersist(string name, string email)
    {
        var mockRepository = new Mock<ICustomerRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);
        var dto = new CustomerDto { Name = name, Email = email };

        var result = await service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Error));
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Never);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WithExistingId_ReturnsSuccess()
    {
        var customer = new Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" };
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);

        var result = await service.GetAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Name, result.Value!.Name);
    }

    [Fact]
    public async Task GetAsync_WithMissingId_ReturnsFailure()
    {
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Customer?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);

        var result = await service.GetAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal("Customer not found", result.Error);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomersAsDtos()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" },
            new() { Id = 2, Name = "Alan Turing", Email = "alan@example.com" },
        };
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingIdAndValidDto_MutatesTrackedEntityAndPersists()
    {
        var customer = new Customer { Id = 1, Name = "Old Name", Email = "old@example.com" };
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = new CustomerService(mockUnitOfWork.Object);
        var dto = new CustomerDto { Name = "New Name", Email = "new@example.com" };

        var result = await service.UpdateAsync(1, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("New Name", customer.Name);
        Assert.Equal("new@example.com", customer.Email);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingId_ReturnsNotFoundEvenWhenDtoIsInvalid()
    {
        // Regression test: UpdateAsync must check existence BEFORE validating the DTO
        // (Task 5) — a missing customer must report "Customer not found", not a
        // field-validation error, even if the DTO itself is also invalid.
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Customer?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);
        var invalidDto = new CustomerDto { Name = "", Email = "" };

        var result = await service.UpdateAsync(999, invalidDto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Customer not found", result.Error);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingIdAndInvalidDto_ReturnsValidationFailureAndDoesNotPersist()
    {
        var customer = new Customer { Id = 1, Name = "Old Name", Email = "old@example.com" };
        var mockRepository = new Mock<ICustomerRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.Customers).Returns(mockRepository.Object);

        var service = new CustomerService(mockUnitOfWork.Object);
        var invalidDto = new CustomerDto { Name = "", Email = "new@example.com" };

        var result = await service.UpdateAsync(1, invalidDto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Old Name", customer.Name);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
