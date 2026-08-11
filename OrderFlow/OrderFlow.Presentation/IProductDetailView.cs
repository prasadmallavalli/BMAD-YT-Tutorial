using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface IProductDetailView
{
    void ShowProduct(ProductDto product);
    void ShowError(string message);
}
