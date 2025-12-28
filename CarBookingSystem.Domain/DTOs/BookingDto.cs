using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarBookingSystem.Domain.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string DriverName { get; set; }
        public string CarMake { get; set; }
        public string CarModel { get; set; }
        public string CarPlate { get; set; }
        public string Pickup { get; set; }
        public string Dropoff { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class LocationSuggestionDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }

    public class BookRideRequest
    {
        public int CustomerId { get; set; }
        public string Pickup { get; set; } = string.Empty;
        public string Dropoff { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BookRideResponse
    {
        public int Id { get; set; }
        public string Pickup { get; set; }
        public string Dropoff { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public bool CarAssigned { get; set; }
        public string DriverName { get; set; }
    }
}