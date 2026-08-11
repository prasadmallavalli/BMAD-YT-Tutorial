using OrderFlow.Domain;

namespace OrderFlow.Exhibits.After.Dip;

public interface ICustomerRepository
{
    Customer? FindById(int id);
}
