using OrderFlow.BLL;

namespace OrderFlow.Presentation;

public interface IProductListView
{
    void DisplayProducts(IReadOnlyList<ProductDto> products);
    void ShowError(string message);
}
