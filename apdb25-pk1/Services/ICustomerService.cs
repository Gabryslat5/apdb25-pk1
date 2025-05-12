using apdb25_pk1.Models;

namespace apdb25_pk1.Services;

public interface ICustomerService
{
    public enum AddRentalResult
    {
        Success,
        NotFound,
        Error
    }
    
    Task<CustomerDTO?> GetCustomerIdAsync(int customer_id, CancellationToken cancellationToken);
    Task<AddRentalResult> AddRentalAsync(int customer_id, RentalPutDTO rentalPutDTO, CancellationToken cancellationToken);
}