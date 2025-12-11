using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RestaurantSystemAPI.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoined", $"{Context.ConnectionId} joined {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserLeft", $"{Context.ConnectionId} left {groupName}");
        }

        // Отправка уведомления конкретной группе
        public async Task SendToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
        }

        // Отправка уведомления всем подключенным клиентам
        public async Task SendToAll(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        // Уведомление о новом заказе (для админов)
        public async Task NotifyNewOrder(object orderData)
        {
            await Clients.Group("Admins").SendAsync("NewOrder", orderData);
        }

        // Уведомление об изменении статуса заказа (для клиента)
        public async Task NotifyOrderStatusChanged(string userId, object orderData)
        {
            await Clients.Group($"User_{userId}").SendAsync("OrderStatusChanged", orderData);
        }

        // Уведомление для курьеров о новом заказе
        public async Task NotifyCouriers(object orderData)
        {
            await Clients.Group("Couriers").SendAsync("NewOrderAvailable", orderData);
        }
    }
}