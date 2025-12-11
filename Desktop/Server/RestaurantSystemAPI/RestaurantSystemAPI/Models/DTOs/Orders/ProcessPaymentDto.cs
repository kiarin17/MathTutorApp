namespace RestaurantSystemAPI.Models.DTOs.Orders
{
    public class ProcessPaymentDto
    {
        public string PaymentMethod { get; set; }
        public PaymentDetailsDto PaymentDetails { get; set; }
    }

    public class PaymentDetailsDto
    {
        public string LastFourDigits { get; set; }
        public string TransactionId { get; set; }
    }
}