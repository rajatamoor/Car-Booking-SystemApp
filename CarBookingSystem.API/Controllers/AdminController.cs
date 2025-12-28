using CarBookingSystem.Domain;
using CarBookingSystem.Domain.DTOs;
using CarBookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CarBookingSystem.API.Hubs;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;

    public AdminController(AppDbContext context, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Manage Drivers (keep existing logic)
    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers()
    {
        var drivers = await _context.Users
            .Where(u => u.Role == "Driver")
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Phone,
                u.LicenseNumber,
                u.Status
            })
            .ToListAsync();
        return Ok(drivers);
    }

    // Create Driver DTO
    public class CreateDriverDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("drivers-create")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        var driver = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber,
            Status = dto.Status,
            Role = "Driver",
            PasswordHash = dto.Password
        };

        _context.Users.Add(driver);
        await _context.SaveChangesAsync();
        return Ok(driver);
    }

    [HttpPut("drivers/{id}")]
    public async Task<IActionResult> UpdateDriver(int id, [FromBody] User updatedDriver)
    {
        var driver = await _context.Users.FindAsync(id);
        if (driver == null) return NotFound();

        driver.Name = updatedDriver.Name;
        driver.Email = updatedDriver.Email;
        driver.Phone = updatedDriver.Phone;
        driver.LicenseNumber = updatedDriver.LicenseNumber;
        driver.Status = updatedDriver.Status;

        await _context.SaveChangesAsync();
        return Ok(driver);
    }

    [HttpDelete("drivers/{id}")]
    public async Task<IActionResult> DeleteDriver(int id)
    {
        var driver = await _context.Users.FindAsync(id);
        if (driver == null) return NotFound();
        _context.Users.Remove(driver);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // Manage Cars (keep existing logic)
    [HttpGet("cars")]
    public async Task<IActionResult> GetCars()
    {
        var cars = await _context.Cars.ToListAsync();
        return Ok(cars);
    }

    [HttpPost("cars")]
    public async Task<IActionResult> AddCar([FromBody] Car car)
    {
        if (car.DriverId.HasValue && !await _context.Users.AnyAsync(u => u.Id == car.DriverId))
        {
            return BadRequest("Invalid Driver ID");
        }

        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
        return Ok(car);
    }

    [HttpPut("cars/{id}")]
    public async Task<IActionResult> UpdateCar(int id, [FromBody] Car updatedCar)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        car.Make = updatedCar.Make;
        car.Model = updatedCar.Model;
        car.Year = updatedCar.Year;
        car.PlateNumber = updatedCar.PlateNumber;
        car.Color = updatedCar.Color;
        car.Type = updatedCar.Type;
        car.Status = updatedCar.Status;
        car.DriverId = updatedCar.DriverId;

        await _context.SaveChangesAsync();
        return Ok(car);
    }

    [HttpDelete("cars/{id}")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();
        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // View all bookings with full info (keep existing logic)
    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Driver)
            .Include(b => b.Car)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                CustomerName = b.Customer.Name,
                DriverName = b.Driver != null ? b.Driver.Name : null,
                CarMake = b.Car != null ? b.Car.Make : null,
                CarModel = b.Car != null ? b.Car.Model : null,
                CarPlate = b.Car != null ? b.Car.PlateNumber : null,
                Pickup = b.Pickup,
                Dropoff = b.Dropoff,
                Status = b.Status,
                Date = b.Date,
                Amount = b.Amount
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // Mark booking as Paid (enhanced with notification)
    [HttpPost("bookings/{id}/mark-paid")]
    public async Task<IActionResult> MarkBookingPaid(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();

        if (booking.Status != "Completed")
            return BadRequest($"Cannot mark as paid. Current status: {booking.Status}");

        booking.Status = "Paid";
        await _context.SaveChangesAsync();

        // Notify customer and driver
        await _hubContext.Clients.All.SendAsync("BookingPaid", new
        {
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer?.Name,
            DriverId = booking.DriverId,
            DriverName = booking.Driver?.Name,
            Amount = booking.Amount,
            PaymentDate = DateTime.Now
        });

        return Ok(new
        {
            Message = "Payment marked successfully",
            BookingId = booking.Id,
            Status = booking.Status
        });
    }

    // NEW: Get system statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalBookings = await _context.Bookings.CountAsync();
        var pendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");
        var acceptedBookings = await _context.Bookings.CountAsync(b => b.Status == "Accepted");
        var completedBookings = await _context.Bookings.CountAsync(b => b.Status == "Completed");
        var paidBookings = await _context.Bookings.CountAsync(b => b.Status == "Paid");
        var totalRevenue = await _context.Bookings
            .Where(b => b.Status == "Paid")
            .SumAsync(b => b.Amount);
        var totalDrivers = await _context.Users.CountAsync(u => u.Role == "Driver");
        var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");

        return Ok(new
        {
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            AcceptedBookings = acceptedBookings,
            CompletedBookings = completedBookings,
            PaidBookings = paidBookings,
            TotalRevenue = totalRevenue,
            TotalDrivers = totalDrivers,
            TotalCustomers = totalCustomers
        });
    }
}