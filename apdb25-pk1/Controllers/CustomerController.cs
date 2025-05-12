using apdb25_pk1.Models;
using apdb25_pk1.Services;
using Microsoft.AspNetCore.Mvc;

namespace apdb25_pk1.Controllers;


[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomerController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet("{id}/rentals")]
    public async Task<IActionResult> GetCustomerRentals(int id, CancellationToken cancellationToken)
    {
        var customer = await _service.GetCustomerIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound($"Customer with ID {id} not found.");
        }

        return Ok(customer);
    }

    [HttpPost("{id}/rentals")]
    public async Task<IActionResult> AddRentals(int id, RentalPutDTO rentalPutDTO, CancellationToken cancellationToken)
    {
        var result = await _service.AddRentalAsync(id, rentalPutDTO, cancellationToken);
        return result switch
        {
            ICustomerService.AddRentalResult.NotFound => NotFound("Customer or one of the movies does not exist."),
            ICustomerService.AddRentalResult.Success => Ok("Rental created successfully."),
            ICustomerService.AddRentalResult.Error => StatusCode(500, "An error occurred during the operation."),
            _ => BadRequest("Invalid request.")
        };
    }
    
}