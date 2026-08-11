using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

public class ProductService : IProductService
{
    private const string NotFoundError = "Product not found";

    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Any Id set on dto is ignored — Create always inserts a new row with a DB-generated Id.
    public async Task<Result<ProductDto>> CreateAsync(ProductDto dto)
    {
        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return Result<ProductDto>.Failure(validationError);
        }

        // Setting the Inventory nav property lets EF Core cascade-insert both rows in one
        // SaveChangesAsync, correctly wiring the FK once Product.Id is generated (Story 1.4
        // Dev Notes) — no need for a two-step insert.
        var product = new Product
        {
            Name = dto.Name.Trim(),
            SKU = dto.SKU.Trim(),
            UnitPrice = dto.UnitPrice,
            Inventory = new Inventory { StockQuantity = dto.StockQuantity },
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<ProductDto>> GetAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product is null
            ? Result<ProductDto>.Failure(NotFoundError)
            : Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        return Result<IReadOnlyList<ProductDto>>.Success(products.Select(ToDto).ToList());
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, ProductDto dto)
    {
        // Fetch before validating (per Story 1.2's UpdateAsync fix) — a missing product
        // must report "Product not found", not a field-validation error, regardless of
        // dto shape.
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
        {
            return Result<ProductDto>.Failure(NotFoundError);
        }

        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return Result<ProductDto>.Failure(validationError);
        }

        // Mutate the tracked entity (and its tracked Inventory navigation) directly — EF's
        // change tracker marks only the changed properties Modified. Never reconstruct a
        // detached graph (AD-6). product.Inventory! is safe under this story's own
        // invariant: every Product is created with an Inventory in the same atomic insert,
        // and nothing here allows creating one without it (Story 1.4 Dev Notes).
        product.Name = dto.Name.Trim();
        product.SKU = dto.SKU.Trim();
        product.UnitPrice = dto.UnitPrice;
        product.Inventory!.StockQuantity = dto.StockQuantity;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<ProductDto>.Failure(ex.Message);
        }

        return Result<ProductDto>.Success(ToDto(product));
    }

    private static string? Validate(ProductDto? dto)
    {
        if (dto is null)
        {
            return "Product data is required";
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        if (dto.Name.Trim().Length > 200)
        {
            return "Name must be 200 characters or fewer";
        }

        if (string.IsNullOrWhiteSpace(dto.SKU))
        {
            return "SKU is required";
        }

        if (dto.SKU.Trim().Length > 50)
        {
            return "SKU must be 50 characters or fewer";
        }

        if (dto.UnitPrice <= 0)
        {
            return "Unit price must be greater than zero";
        }

        if (dto.StockQuantity < 0)
        {
            return "Stock quantity cannot be negative";
        }

        return null;
    }

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        SKU = product.SKU,
        UnitPrice = product.UnitPrice,
        StockQuantity = product.Inventory?.StockQuantity ?? 0,
    };
}
