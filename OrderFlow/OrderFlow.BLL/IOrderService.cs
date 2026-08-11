namespace OrderFlow.BLL;

// Read-only — Order creation goes through IOrderProcessor (Story 2.5), status changes go
// through IOrderStatusService (Story 2.4/3.3). This service never mutates an Order.
public interface IOrderService
{
    Task<Result<OrderDto>> GetAsync(int id);
    Task<Result<IReadOnlyList<OrderDto>>> GetAllAsync();
}
