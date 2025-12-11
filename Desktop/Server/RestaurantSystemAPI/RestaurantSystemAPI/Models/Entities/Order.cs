using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantSystemAPI.Models.DTOs.Orders;

namespace RestaurantSystemAPI.Models.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cooking, AssignedToCourier, OnTheWay, Delivered, Cancelled

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public string DeliveryAddress { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }
        public string? SpecialInstructions { get; set; }
        public string PaymentMethod { get; set; } = "cash"; // "cash" или "card"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? AssignedCourierId { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }

        // Навигационные свойства
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("AssignedCourierId")]
        public virtual User? AssignedCourier { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; }
    }
}