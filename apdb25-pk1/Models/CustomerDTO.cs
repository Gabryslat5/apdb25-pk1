namespace apdb25_pk1.Models;

public class CustomerDTO
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public List<RentalDTO> rentals { get; set; }
}