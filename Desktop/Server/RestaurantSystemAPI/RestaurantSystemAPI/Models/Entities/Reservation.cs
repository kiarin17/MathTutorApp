using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantSystemAPI.Models.Entities
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        // Убираем StringLength для LONGTEXT
        public string Type { get; set; } = "Бронирование стола";

        [Required]
        public DateTime ReservationDateTime { get; set; }

        [Required]
        [Range(1, 50)]
        public int GuestsCount { get; set; }

        [Required]
        public string Status { get; set; } = "pending";

        public string? SpecialRequests { get; set; }

        // Убираем StringLength для LONGTEXT полей
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string CustomerPhone { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        // Новые поля
        public string? AdditionalServices { get; set; }
        public bool IsTasting { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационное свойство
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}