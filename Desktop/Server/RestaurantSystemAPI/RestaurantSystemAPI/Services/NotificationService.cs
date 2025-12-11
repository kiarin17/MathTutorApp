using Microsoft.AspNetCore.SignalR;
using RestaurantSystemAPI.Hubs;

namespace RestaurantSystemAPI.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // Уведомление о новом заказе для админов
        public async Task NotifyNewOrderAsync(object orderData)
        {
            await _hubContext.Clients.Group("Admins").SendAsync("NewOrder", orderData);
        }

        // Уведомление об изменении статуса заказа для клиента
        public async Task NotifyOrderStatusChangedAsync(int userId, object orderData)
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("OrderStatusChanged", orderData);
        }

        // Уведомление для курьеров о новом заказе
        public async Task NotifyCouriersAsync(object orderData)
        {
            await _hubContext.Clients.Group("Couriers").SendAsync("NewOrderAvailable", orderData);
        }

        // Уведомление о новом бронировании для админов
        public async Task NotifyNewReservationAsync(object reservationData)
        {
            await _hubContext.Clients.Group("Admins").SendAsync("NewReservation", reservationData);
        }

        // Уведомление об изменении статуса бронирования
        public async Task NotifyReservationStatusChangedAsync(int userId, object reservationData)
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReservationStatusChanged", reservationData);
        }
    }
}