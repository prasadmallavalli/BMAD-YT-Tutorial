namespace OrderFlow.BLL;

public interface IProductService
{
    Task<Result<ProductDto>> CreateAsync(ProductDto dto);
    Task<Result<ProductDto>> GetAsync(int id);
    Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync();
    Task<Result<ProductDto>> UpdateAsync(int id, ProductDto dto);
}
