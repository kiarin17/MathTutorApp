using System.Collections.Generic;

namespace RestaurantSystemAPI.Models.DTOs.Orders
{
    public class CreateOrderDto
    {
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string SpecialInstructions { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
    }
}