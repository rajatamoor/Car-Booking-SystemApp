using CarBookingSystem.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace CarBookingSystem.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.LicenseNumber).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                // Relationships
                entity.HasMany(u => u.CustomerBookings)
                      .WithOne(b => b.Customer)
                      .HasForeignKey(b => b.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.DriverBookings)
                      .WithOne(b => b.Driver)
                      .HasForeignKey(b => b.DriverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Car entity
            modelBuilder.Entity<Car>(entity =>
            {
                entity.ToTable("Cars");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Make).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Model).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.PlateNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DriverId).IsRequired(false);

                // Relationships
                entity.HasOne(e => e.Driver)
                      .WithMany()
                      .HasForeignKey(e => e.DriverId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Bookings)
                      .WithOne(b => b.Car)
                      .HasForeignKey(b => b.CarId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Pickup).IsRequired();
                entity.Property(e => e.Dropoff).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.CarId).IsRequired(false);
                entity.Property(e => e.DriverId).IsRequired(false);
                entity.Property(e => e.CustomerId).IsRequired();

                // Relationships - make them optional
                entity.HasOne(e => e.Customer)
                      .WithMany(u => u.CustomerBookings)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasOne(e => e.Driver)
                      .WithMany(u => u.DriverBookings)
                      .HasForeignKey(e => e.DriverId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.HasOne(e => e.Car)
                      .WithMany(c => c.Bookings)
                      .HasForeignKey(e => e.CarId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });
        }
    }
}