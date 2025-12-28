using CarBookingSystem.Domain;
using CarBookingSystem.Domain.DTOs;
using CarBookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CarBookingSystem.API.Hubs;
using Microsoft.EntityFrameworkCore.Storage;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(AppDbContext context, IHubContext<BookingHub> hubContext,
                            IHttpClientFactory httpClientFactory, ILogger<CustomerController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // Get location suggestions
    [HttpGet("locations")]
    public async Task<IActionResult> GetLocationSuggestions([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            return Ok(new List<LocationSuggestionDto>());

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}&limit=5&addressdetails=1";

            client.DefaultRequestHeaders.UserAgent.ParseAdd("CarBookingSystem/1.0");

            var response = await client.GetFromJsonAsync<List<dynamic>>(url);

            var suggestions = response?.Select(r => new LocationSuggestionDto
            {
                DisplayName = r.display_name?.ToString() ?? "",
                Lat = r.lat?.ToString() ?? "",
                Lon = r.lon?.ToString() ?? ""
            }).ToList();

            return Ok(suggestions ?? new List<LocationSuggestionDto>());
        }
        catch
        {
            return Ok(new List<LocationSuggestionDto>());
        }
    }

    // Book a ride
    [HttpPost("book")]
    public async Task<IActionResult> BookRide([FromBody] BookRideRequest bookingRequest)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            _logger.LogInformation($"Booking ride for customer: {bookingRequest.CustomerId}");
            _logger.LogInformation($"Pickup: {bookingRequest.Pickup}");
            _logger.LogInformation($"Dropoff: {bookingRequest.Dropoff}");
            _logger.LogInformation($"Amount: {bookingRequest.Amount}");

            // Validate required fields
            if (bookingRequest.CustomerId <= 0)
                return BadRequest("Invalid customer ID");

            if (string.IsNullOrWhiteSpace(bookingRequest.Pickup))
                return BadRequest("Pickup location is required");

            if (string.IsNullOrWhiteSpace(bookingRequest.Dropoff))
                return BadRequest("Dropoff location is required");

            if (bookingRequest.Amount < 5)
                return BadRequest("Amount must be at least $5");

            // Check if customer exists
            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == bookingRequest.CustomerId && u.Role == "Customer");

            if (customer == null)
            {
                _logger.LogWarning($"Customer with ID {bookingRequest.CustomerId} not found or not a customer");
                return BadRequest($"Customer with ID {bookingRequest.CustomerId} not found");
            }

            Booking? booking = null;
            Car? availableCar = null;

            // Execution strategy for handling retries
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create Booking entity with default values
                    booking = new Booking
                    {
                        CustomerId = bookingRequest.CustomerId,
                        Pickup = bookingRequest.Pickup,
                        Dropoff = bookingRequest.Dropoff,
                        Amount = bookingRequest.Amount,
                        Status = "Pending",
                        Date = DateTime.Now
                    };

                    _logger.LogInformation($"Created booking entity");

                    // Try to find an available car with an active driver
                    availableCar = await _context.Cars
                        .Include(c => c.Driver)
                        .Where(c => c.Status == "Available" &&
                                   c.Driver != null &&
                                   c.Driver.Status == "Active" &&
                                   c.Driver.Role == "Driver")
                        .FirstOrDefaultAsync();

                    _logger.LogInformation($"Available car found: {availableCar != null}");

                    if (availableCar != null && availableCar.DriverId.HasValue)
                    {
                        booking.CarId = availableCar.Id;
                        booking.DriverId = availableCar.DriverId.Value;

                        // Update car status
                        availableCar.Status = "Busy";
                        _context.Cars.Update(availableCar);

                        _logger.LogInformation($"Assigned car {availableCar.Id} and driver {availableCar.DriverId.Value} to booking");
                    }
                    else
                    {
                        _logger.LogInformation("No available car found, booking will be created without assignment");
                        // Leave CarId and DriverId as null (they're nullable now)
                    }

                    // Add booking to context
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Booking saved successfully with ID: {booking.Id}");

                    // Commit transaction
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            if (booking == null) return StatusCode(500, "Booking failed to be created.");

            // Notify drivers about new booking
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNewBooking", booking.Id);
                _logger.LogInformation($"SignalR notification sent for booking {booking.Id}");
            }
            catch (Exception hubEx)
            {
                 _logger.LogWarning(hubEx, "Failed to send SignalR notification, but booking was saved");
            }

            // Return response
            var response = new BookRideResponse
            {
                Id = booking.Id,
                Pickup = booking.Pickup,
                Dropoff = booking.Dropoff,
                Status = booking.Status,
                Amount = booking.Amount,
                Date = booking.Date,
                Message = availableCar != null ?
                    "Ride booked successfully! Driver assigned." :
                    "Ride booked successfully! Waiting for available driver.",
                CarAssigned = availableCar != null,
                DriverName = availableCar?.Driver?.Name ?? "Not assigned yet"
            };

            _logger.LogInformation($"Returning booking response for ID: {booking.Id}");
            return Ok(response);


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error booking ride: {ex.Message}");
            return StatusCode(500, $"Error booking ride: {ex.Message}");
        }
    }

    // View my bookings
    [HttpGet("{customerId}/bookings")]
    public async Task<IActionResult> GetMyBookings(int customerId)
    {
        try
        {
            var bookings = await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .Include(b => b.Customer)
                .Include(b => b.Driver)
                .Include(b => b.Car)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    CustomerName = b.Customer.Name,
                    DriverName = b.Driver != null ? b.Driver.Name : "Not assigned",
                    CarMake = b.Car != null ? b.Car.Make : "Not assigned",
                    CarModel = b.Car != null ? b.Car.Model : "Not assigned",
                    CarPlate = b.Car != null ? b.Car.PlateNumber : "Not assigned",
                    Pickup = b.Pickup,
                    Dropoff = b.Dropoff,
                    Status = b.Status,
                    Date = b.Date,
                    Amount = b.Amount
                })
                .OrderByDescending(b => b.Date)
                .Take(10)
                .ToListAsync();

            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting bookings for customer {customerId}: {ex.Message}");
            return StatusCode(500, $"Error getting bookings: {ex.Message}");
        }
    }

    // Test endpoint to check database and create a test booking
    [HttpGet("test-database")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            _logger.LogInformation("Testing database connection...");

            // Test connection
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(500, "Cannot connect to database");
            }

            // Check table counts
            var usersCount = await _context.Users.CountAsync();
            var carsCount = await _context.Cars.CountAsync();
            var bookingsCount = await _context.Bookings.CountAsync();

            // Check schema for Bookings table
            var bookingProperties = typeof(Booking).GetProperties();
            var propertyInfo = new List<object>();

            foreach (var prop in bookingProperties)
            {
                propertyInfo.Add(new
                {
                    Name = prop.Name,
                    Type = prop.PropertyType.Name,
                    IsNullable = IsNullable(prop.PropertyType)
                });
            }

            // Try a simple insert (without saving)
            var testCustomer = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Customer");

            if (testCustomer == null)
            {
                return Ok(new
                {
                    DatabaseConnected = canConnect,
                    UsersCount = usersCount,
                    CarsCount = carsCount,
                    BookingsCount = bookingsCount,
                    TestCustomerFound = false,
                    Message = "No customer found for test",
                    BookingProperties = propertyInfo
                });
            }

            // Test creating a booking (in memory only)
            var testBooking = new Booking
            {
                CustomerId = testCustomer.Id,
                Pickup = "Test Location",
                Dropoff = "Test Destination",
                Amount = 10.00m,
                Status = "Pending",
                Date = DateTime.Now
            };

            // Validate the booking
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(testBooking);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(testBooking, validationContext, validationResults, true);

            return Ok(new
            {
                DatabaseConnected = canConnect,
                UsersCount = usersCount,
                CarsCount = carsCount,
                BookingsCount = bookingsCount,
                TestCustomerFound = true,
                TestCustomerId = testCustomer.Id,
                TestBookingValid = isValid,
                ValidationErrors = validationResults.Select(v => v.ErrorMessage),
                BookingProperties = propertyInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new
            {
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private bool IsNullable(Type type)
    {
        return !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
    }
}