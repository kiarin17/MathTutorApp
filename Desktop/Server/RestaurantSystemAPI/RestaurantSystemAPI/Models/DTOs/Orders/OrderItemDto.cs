namespace RestaurantSystemAPI.Models.DTOs.Orders
{
    public class OrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ItemName { get; set; }
    }
}