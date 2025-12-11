using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;
using RestaurantSystemAPI.Services;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public OrdersController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/orders
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.AssignedCourier)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.DeliveryAddress,
                    o.CreatedAt,
                    User = new { o.User.FirstName, o.User.LastName, o.User.Email },
                    Courier = o.AssignedCourier != null ? new { o.AssignedCourier.FirstName, o.AssignedCourier.LastName } : null,
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ItemName,
                        oi.Quantity,
                        oi.UnitPrice,
                        Total = oi.Quantity * oi.UnitPrice
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/orders/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.AssignedCourier)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                order.Id,
                order.Status,
                order.TotalAmount,
                order.DeliveryAddress,
                order.CustomerName,
                order.CustomerPhone,
                order.SpecialInstructions,
                order.CreatedAt,
                order.EstimatedDeliveryTime,
                User = new { order.User.FirstName, order.User.LastName, order.User.Email },
                Courier = order.AssignedCourier != null ? new { order.AssignedCourier.FirstName, order.AssignedCourier.LastName } : null,
                Items = order.OrderItems.Select(oi => new
                {
                    oi.ItemName,
                    oi.Quantity,
                    oi.UnitPrice,
                    Total = oi.Quantity * oi.UnitPrice
                })
            });
        }

        // POST: api/orders
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Находим пользователя
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return BadRequest(new { message = "Пользователь не найден" });
                }

                // Проверяем и рассчитываем товары
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in request.Items)
                {
                    var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                    if (menuItem == null || !menuItem.IsAvailable)
                    {
                        return BadRequest(new { message = $"Товар с ID {item.MenuItemId} не найден или недоступен" });
                    }

                    var orderItem = new OrderItem
                    {
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = menuItem.Price,
                        ItemName = menuItem.Name
                    };

                    orderItems.Add(orderItem);
                    totalAmount += item.Quantity * menuItem.Price;
                }

                // Создаем заказ
                var order = new Order
                {
                    UserId = request.UserId,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    DeliveryAddress = request.DeliveryAddress,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    CustomerEmail = request.CustomerEmail,
                    SpecialInstructions = request.SpecialInstructions,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Уведомление о новом заказе для админов и курьеров
                var orderNotification = new
                {
                    order.Id,
                    order.CustomerName,
                    order.TotalAmount,
                    order.DeliveryAddress,
                    order.CreatedAt,
                    ItemsCount = order.OrderItems.Count,
                    Status = order.Status
                };

                await _notificationService.NotifyNewOrderAsync(orderNotification);
                await _notificationService.NotifyCouriersAsync(orderNotification);

                return Ok(new
                {
                    message = "Заказ успешно создан",
                    orderId = order.Id,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/orders/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Courier")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                var oldStatus = order.Status;
                order.Status = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

                // Если статус "AssignedToCourier", назначаем курьера
                if (request.Status == "AssignedToCourier" && request.CourierId.HasValue)
                {
                    order.AssignedCourierId = request.CourierId.Value;
                }

                // Добавляем запись в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = id,
                    Status = request.Status,
                    Notes = request.Notes
                };

                _context.OrderStatusHistory.Add(statusHistory);
                await _context.SaveChangesAsync();

                // Уведомление об изменении статуса заказа для клиента
                var statusNotification = new
                {
                    OrderId = id,
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    UpdatedAt = DateTime.UtcNow,
                    order.CustomerName,
                    Notes = request.Notes
                };

                await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, statusNotification);

                // Если заказ доставлен, уведомляем админов
                if (request.Status == "Delivered")
                {
                    var deliveryNotification = new
                    {
                        OrderId = id,
                        order.CustomerName,
                        order.TotalAmount,
                        DeliveredAt = DateTime.UtcNow
                    };
                    await _notificationService.NotifyNewOrderAsync(deliveryNotification);
                }

                return Ok(new { message = "Статус заказа обновлен" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/orders/user/5 - заказы конкретного пользователя
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .Include(o => o.AssignedCourier)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.DeliveryAddress,
                    o.CreatedAt,
                    o.EstimatedDeliveryTime,
                    Courier = o.AssignedCourier != null ? new { o.AssignedCourier.FirstName, o.AssignedCourier.LastName } : null,
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ItemName,
                        oi.Quantity,
                        oi.UnitPrice
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/orders/courier/5 - заказы конкретного курьера
        [HttpGet("courier/{courierId}")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> GetCourierOrders(int courierId)
        {
            var orders = await _context.Orders
                .Where(o => o.AssignedCourierId == courierId)
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.DeliveryAddress,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.CreatedAt,
                    o.EstimatedDeliveryTime,
                    Customer = new { o.User.FirstName, o.User.LastName },
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ItemName,
                        oi.Quantity
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }
    }

    // DTO классы для заказов
    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public string DeliveryAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string SpecialInstructions { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
        public int? CourierId { get; set; }
        public string Notes { get; set; }
    }
}