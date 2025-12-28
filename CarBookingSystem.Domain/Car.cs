using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarBookingSystem.Domain
{
    public class Car
    {
        public int Id { get; set; }
        public int? DriverId { get; set; } // Nullable because a car might not have a driver yet
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string PlateNumber { get; set; }
        public string Color { get; set; }
        public string Type { get; set; }
        public string Status { get; set; } // Available, Booked, Maintenance, OutOfService

        public virtual User? Driver { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
