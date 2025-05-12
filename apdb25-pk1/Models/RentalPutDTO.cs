namespace apdb25_pk1.Models;

public class RentalPutDTO
{
    public int id { get; set; }
    public DateTime rentalDate { get; set; }
    public List<MovieDTO> movies { get; set; }
}