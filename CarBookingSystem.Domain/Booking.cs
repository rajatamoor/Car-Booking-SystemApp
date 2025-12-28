using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarBookingSystem.Domain
{
    public class Booking
    {
        public int Id { get; set; }

        // Make these nullable to allow booking without immediate assignment
        public int? CarId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? DriverId { get; set; }

        [Required]
        public string Pickup { get; set; } = string.Empty;

        [Required]
        public string Dropoff { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Navigation properties
        public virtual User Customer { get; set; } = null!;
        public virtual User? Driver { get; set; }
        public virtual Car? Car { get; set; }
    }
}