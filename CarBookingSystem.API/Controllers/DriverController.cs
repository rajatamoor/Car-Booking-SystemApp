using CarBookingSystem.Domain;
using CarBookingSystem.Domain.DTOs;
using CarBookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CarBookingSystem.API.Hubs;

[ApiController]
[Route("api/[controller]")]
public class DriverController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;

    public DriverController(AppDbContext context, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // View pending rides (keep existing logic)
    [HttpGet("pending-rides")]
    public async Task<IActionResult> GetPendingRides()
    {
        var rides = await _context.Bookings
            .Where(b => b.Status == "Pending")
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

        return Ok(rides);
    }

    // Accept a ride (keep existing logic)
    [HttpPost("{driverId}/accept/{bookingId}")]
    public async Task<IActionResult> AcceptRide(int driverId, int bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) return NotFound("Booking not found");

        booking.DriverId = driverId;
        booking.Status = "Accepted";
        await _context.SaveChangesAsync();

        // Notify everyone (especially the customer)
        await _hubContext.Clients.All.SendAsync("ReceiveRideAccepted", bookingId, driverId);

        return Ok(booking);
    }

    // NEW: Complete a ride
    [HttpPost("{driverId}/complete/{bookingId}")]
    public async Task<IActionResult> CompleteRide(int driverId, int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Car)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.DriverId == driverId);

        if (booking == null) return NotFound("Booking not found");

        if (booking.Status != "Accepted")
            return BadRequest($"Cannot complete ride. Current status: {booking.Status}");

        booking.Status = "Completed";

        // Free up the car
        if (booking.Car != null)
        {
            booking.Car.Status = "Available";
        }

        await _context.SaveChangesAsync();

        // Notify admin that ride is ready for payment
        await _hubContext.Clients.All.SendAsync("RideCompleted", bookingId);

        return Ok(new { Message = "Ride completed successfully", BookingId = bookingId });
    }

    // View accepted rides (keep existing logic)
    [HttpGet("{driverId}/accepted-rides")]
    public async Task<IActionResult> GetAcceptedRides(int driverId)
    {
        var rides = await _context.Bookings
            .Where(b => b.DriverId == driverId && b.Status == "Accepted")
            .Include(b => b.Customer)
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

        return Ok(rides);
    }

    // NEW: Get completed rides
    [HttpGet("{driverId}/completed-rides")]
    public async Task<IActionResult> GetCompletedRides(int driverId)
    {
        var rides = await _context.Bookings
            .Where(b => b.DriverId == driverId && b.Status == "Completed")
            .Include(b => b.Customer)
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

        return Ok(rides);
    }
}