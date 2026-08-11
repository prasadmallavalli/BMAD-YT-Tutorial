using OrderFlow.DAL;
using OrderFlow.Domain;

namespace OrderFlow.BLL;

public class CustomerService : ICustomerService
{
    private const string NotFoundError = "Customer not found";

    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Any Id set on dto is ignored — Create always inserts a new row with a DB-generated Id.
    public async Task<Result<CustomerDto>> CreateAsync(CustomerDto dto)
    {
        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return Result<CustomerDto>.Failure(validationError);
        }

        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim(),
        };

        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return Result<CustomerDto>.Success(ToDto(customer));
    }

    public async Task<Result<CustomerDto>> GetAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        return customer is null
            ? Result<CustomerDto>.Failure(NotFoundError)
            : Result<CustomerDto>.Success(ToDto(customer));
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        return Result<IReadOnlyList<CustomerDto>>.Success(customers.Select(ToDto).ToList());
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, CustomerDto dto)
    {
        // Fetch before validating (per Task 5) — a missing customer must report
        // "Customer not found", not a field-validation error, regardless of dto shape.
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer is null)
        {
            return Result<CustomerDto>.Failure(NotFoundError);
        }

        var validationError = Validate(dto);
        if (validationError is not null)
        {
            return Result<CustomerDto>.Failure(validationError);
        }

        // Mutate the tracked entity directly — EF's change tracker marks only the changed
        // properties Modified. Never reconstruct/reattach a detached graph (AD-6).
        customer.Name = dto.Name.Trim();
        customer.Email = dto.Email.Trim();
        customer.Phone = dto.Phone?.Trim();

        await _unitOfWork.SaveChangesAsync();

        return Result<CustomerDto>.Success(ToDto(customer));
    }

    private static string? Validate(CustomerDto? dto)
    {
        if (dto is null)
        {
            return "Customer data is required";
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        if (dto.Name.Trim().Length > 200)
        {
            return "Name must be 200 characters or fewer";
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return "Email is required";
        }

        if (dto.Email.Trim().Length > 256)
        {
            return "Email must be 256 characters or fewer";
        }

        if (dto.Phone?.Trim().Length > 30)
        {
            return "Phone must be 30 characters or fewer";
        }

        return null;
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone,
    };
}
