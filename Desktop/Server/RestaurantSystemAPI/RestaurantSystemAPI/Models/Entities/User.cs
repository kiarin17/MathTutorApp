using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantSystemAPI.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = UserRole.Client;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Только для курьеров
        public string? VehicleType { get; set; }
        public string? VehiclePlate { get; set; }
        public bool? IsAvailable { get; set; }

        // Навигационные свойства
        public virtual ICollection<Reservation> Reservations { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Order> AssignedOrders { get; set; }
    }
}