using System;
using System.Collections.Generic;

namespace RestaurantSystemAPI.Models.DTOs.Orders
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string SpecialInstructions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public int? AssignedCourierId { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemResponseDto> OrderItems { get; set; }
    }
}