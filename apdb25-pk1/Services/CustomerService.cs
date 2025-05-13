using apdb25_pk1.Models;
using Microsoft.Data.SqlClient;

namespace apdb25_pk1.Services;

public class CustomerService : ICustomerService
{
    //private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True;";
    private readonly string _connectionString;
    public CustomerService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    
    private ICustomerService _customerServiceImplementation;
    
    public async Task<CustomerDTO> GetCustomerIdAsync(int customer_id, CancellationToken cancellationToken)
    {
        CustomerDTO? customer = null;
        var rentalsDict = new Dictionary<int, RentalDTO>();

        string command = @"SELECT 
            c.first_name, 
            c.last_name, 
            r.rental_id, 
            r.rental_date, 
            r.return_date, 
            CASE
                WHEN r.return_date IS NULL THEN 'Rented'
                ELSE 'Returned'
            END AS 'status',
            m.title,
            ri.price_at_rental 
            FROM Customer c   
            LEFT JOIN Rental r ON r.customer_id = c.customer_id                        
            LEFT JOIN Rental_Item ri ON ri.rental_id = r.rental_id
            LEFT JOIN Movie m ON m.movie_id = ri.movie_id
            WHERE c.customer_id = @id";
        
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(command, conn);
        cmd.Parameters.AddWithValue("@id", customer_id);
        await conn.OpenAsync(cancellationToken);
        
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (customer == null)
            {
                customer = new CustomerDTO
                {
                    firstName = reader.GetString(reader.GetOrdinal("first_name")),
                    lastName = reader.GetString(reader.GetOrdinal("last_name")),
                    rentals = new List<RentalDTO>()
                };
            }

            if (!reader.IsDBNull(reader.GetOrdinal("rental_id")))
            {
                var rentalId = reader.GetInt32(reader.GetOrdinal("rental_id"));
                if (!rentalsDict.ContainsKey(rentalId))
                {
                    var rental = new RentalDTO
                    {
                        id = rentalId,
                        rentalDate = reader.GetDateTime(reader.GetOrdinal("rental_date")),
                        returnDate = reader.IsDBNull(reader.GetOrdinal("return_date"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("return_date")),
                        status = reader.GetString(reader.GetOrdinal("status")),
                        Movies = new List<MovieDTO>()
                    };
                    rentalsDict[rentalId] = rental;
                    customer.rentals.Add(rental);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("title")))
                {
                    rentalsDict[rentalId].Movies.Add(new MovieDTO
                    {
                        title = reader.GetString(reader.GetOrdinal("title")),
                        priceAtRental = reader.GetDecimal(reader.GetOrdinal("price_at_rental"))
                    });
                }
            }
        }

        return customer;
        
    }

    public async Task<ICustomerService.AddRentalResult> AddRentalAsync(int customer_id, RentalPutDTO rentalPutDTO, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        using var tran = conn.BeginTransaction();

        try
        {
            // Czy istnieje klient
            var cmd = new SqlCommand("SELECT 1 FROM CUSTOMER c WHERE c.customer_id = @id", conn, tran);
            cmd.Parameters.AddWithValue("@id", customer_id);
            var exists = await cmd.ExecuteScalarAsync(cancellationToken);
            if (exists == null)
            {
                return ICustomerService.AddRentalResult.NotFound;
            }
            
            // Czy filmy istnieją
            foreach (var movie in rentalPutDTO.movies)
            {
                var movieCmd = new SqlCommand("SELECT 1 FROM Movie WHERE title = @title", conn, tran);
                movieCmd.Parameters.AddWithValue("@title", movie.title);
                var movieExists = await movieCmd.ExecuteScalarAsync(cancellationToken);
                if (movieExists == null)
                    return ICustomerService.AddRentalResult.NotFound;
            }
            
            // Dodaj wypożyczenie
            var statusCmd = new SqlCommand("SELECT status_id FROM Status WHERE name = @status", conn, tran);
            statusCmd.Parameters.AddWithValue("@status", "Rented");
            var statusObj = await statusCmd.ExecuteScalarAsync(cancellationToken);
            if (statusObj == null) return ICustomerService.AddRentalResult.Error;
            int statusId = (int)statusObj;

            
            var insertRentalCmd = new SqlCommand(
                "INSERT INTO Rental (rental_id, rental_date, return_date, customer_id, status_id) VALUES (@id, @rentalDate, NULL, @customerId, @statusId)",
                conn, tran);
            insertRentalCmd.Parameters.AddWithValue("@id", rentalPutDTO.id);
            insertRentalCmd.Parameters.AddWithValue("@rentalDate", rentalPutDTO.rentalDate);
            insertRentalCmd.Parameters.AddWithValue("@customerId", customer_id);
            insertRentalCmd.Parameters.AddWithValue("@statusId", statusId);
            await insertRentalCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Dodaj elementy wypożyczenia
            foreach (var movie in rentalPutDTO.movies)
            {
                // Pobierz ID filmu
                var movieIdCmd = new SqlCommand("SELECT movie_id FROM Movie WHERE title = @title", conn, tran);
                movieIdCmd.Parameters.AddWithValue("@title", movie.title);
                var movieIdObj = await movieIdCmd.ExecuteScalarAsync(cancellationToken);
                if (movieIdObj == null) return ICustomerService.AddRentalResult.NotFound;
                int movieId = (int)movieIdObj;
        
                var insertItemCmd = new SqlCommand(
                    "INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental) VALUES (@rentalId, @movieId, @price)",
                    conn, tran);
                insertItemCmd.Parameters.AddWithValue("@rentalId", rentalPutDTO.id);
                insertItemCmd.Parameters.AddWithValue("@movieId", movieId);
                insertItemCmd.Parameters.AddWithValue("@price", movie.priceAtRental);
                await insertItemCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        
            // 5. Zatwierdź transakcję
            await tran.CommitAsync(cancellationToken);
            return ICustomerService.AddRentalResult.Success;
        }
        catch(Exception ex)
        {
            await tran.RollbackAsync(cancellationToken);
            Console.WriteLine($"ERROR: {ex.Message}");
            return ICustomerService.AddRentalResult.Error;
        }
        
    }
}