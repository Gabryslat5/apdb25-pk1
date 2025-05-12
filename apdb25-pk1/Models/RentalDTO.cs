namespace apdb25_pk1.Models;

public class RentalDTO
{
    public int id { get; set; }
    public DateTime rentalDate { get; set; }
    public DateTime? returnDate { get; set; }
    public string status { get; set; }
    
    public List<MovieDTO> Movies { get; set; }
}