using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarBookingSystem.Domain
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; // Customer / Driver / Admin

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; // Active, Inactive, OnLeave

        // Navigation properties - ADD THESE
        public virtual ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
        public virtual ICollection<Booking> DriverBookings { get; set; } = new List<Booking>();


    }
}