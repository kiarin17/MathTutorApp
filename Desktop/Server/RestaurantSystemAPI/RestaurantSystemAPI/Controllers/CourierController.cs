using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;
using RestaurantSystemAPI.Services;
using System.Security.Claims;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Courier")]
    public class CourierController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public CourierController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/courier/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetCourierDashboard()
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var courier = await _context.Users.FindAsync(courierId);
                if (courier == null)
                {
                    return Unauthorized();
                }

                var today = DateTime.Today;

                var stats = new
                {
                    TotalOrders = await _context.Orders
                        .CountAsync(o => o.AssignedCourierId == courierId),

                    TodayOrders = await _context.Orders
                        .CountAsync(o => o.AssignedCourierId == courierId &&
                                        o.CreatedAt.Date == today),

                    DeliveredToday = await _context.Orders
                        .CountAsync(o => o.AssignedCourierId == courierId &&
                                        o.Status == "Delivered" &&
                                        o.UpdatedAt.Date == today),

                    ActiveOrders = await _context.Orders
                        .CountAsync(o => o.AssignedCourierId == courierId &&
                                        (o.Status == "AssignedToCourier" || o.Status == "OnTheWay")),

                    TotalEarnings = await _context.Orders
                        .Where(o => o.AssignedCourierId == courierId &&
                                   o.Status == "Delivered" &&
                                   o.UpdatedAt.Date == today)
                        .SumAsync(o => o.TotalAmount * 0.1m) // 10% от суммы заказа
                };

                return Ok(new
                {
                    Courier = new
                    {
                        courier.FirstName,
                        courier.LastName,
                        courier.VehicleType,
                        courier.VehiclePlate,
                        courier.IsAvailable
                    },
                    Stats = stats
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/courier/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetCourierOrders()
        {
            var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var orders = await _context.Orders
                .Where(o => o.AssignedCourierId == courierId)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.DeliveryAddress,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.SpecialInstructions,
                    o.CreatedAt,
                    o.EstimatedDeliveryTime,
                    Customer = new { o.User.FirstName, o.User.LastName },
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ItemName,
                        oi.Quantity
                    }),
                    CanStartDelivery = o.Status == "AssignedToCourier",
                    CanCompleteDelivery = o.Status == "OnTheWay"
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/courier/available-orders
        [HttpGet("available-orders")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            var availableOrders = await _context.Orders
                .Where(o => o.Status == "Confirmed" || o.Status == "Cooking")
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.DeliveryAddress,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.CreatedAt,
                    EstimatedPreparationTime = o.OrderItems.Sum(oi => oi.MenuItem.PreparationTime ?? 0) + 10, // +10 минут на упаковку
                    ItemsCount = o.OrderItems.Count,
                    RestaurantLocation = "ул. Ресторанная, 1" // Статичное местоположение ресторана
                })
                .ToListAsync();

            return Ok(availableOrders);
        }

        // PUT: api/courier/take-order/5
        [HttpPut("take-order/{orderId}")]
        public async Task<IActionResult> TakeOrder(int orderId)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                if (order.Status != "Confirmed" && order.Status != "Cooking")
                {
                    return BadRequest(new { message = "Заказ недоступен для принятия" });
                }

                // Проверяем, не занят ли курьер другими заказами
                var activeOrdersCount = await _context.Orders
                    .CountAsync(o => o.AssignedCourierId == courierId &&
                                    (o.Status == "AssignedToCourier" || o.Status == "OnTheWay"));

                if (activeOrdersCount >= 3) // Максимум 3 активных заказа
                {
                    return BadRequest(new { message = "У вас слишком много активных заказов" });
                }

                order.AssignedCourierId = courierId;
                order.Status = "AssignedToCourier";
                order.UpdatedAt = DateTime.UtcNow;

                // Добавляем в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = "AssignedToCourier",
                    ChangedById = courierId,
                    Notes = "Курьер принял заказ"
                };

                _context.OrderStatusHistory.Add(statusHistory);

                // Обновляем статус курьера на "занят"
                var courier = await _context.Users.FindAsync(courierId);
                if (courier != null)
                {
                    courier.IsAvailable = false;
                }

                await _context.SaveChangesAsync();

                // Уведомление клиенту о том, что курьер принял заказ
                var notification = new
                {
                    OrderId = orderId,
                    Status = "AssignedToCourier",
                    Message = "Курьер принял ваш заказ",
                    CourierName = $"{courier.FirstName} {courier.LastName}",
                    CourierVehicle = $"{courier.VehicleType} ({courier.VehiclePlate})",
                    UpdatedAt = DateTime.UtcNow
                };

                await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, notification);

                return Ok(new { message = "Заказ успешно принят" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/courier/start-delivery/5
        [HttpPut("start-delivery/{orderId}")]
        public async Task<IActionResult> StartDelivery(int orderId)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.AssignedCourierId == courierId);

                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                if (order.Status != "AssignedToCourier")
                {
                    return BadRequest(new { message = "Невозможно начать доставку для этого заказа" });
                }

                order.Status = "OnTheWay";
                order.UpdatedAt = DateTime.UtcNow;
                order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(30); // Расчетное время доставки

                // Добавляем в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = "OnTheWay",
                    ChangedById = courierId,
                    Notes = "Курьер начал доставку"
                };

                _context.OrderStatusHistory.Add(statusHistory);
                await _context.SaveChangesAsync();

                // Уведомление клиенту
                var notification = new
                {
                    OrderId = orderId,
                    Status = "OnTheWay",
                    Message = "Курьер выехал к вам",
                    EstimatedDeliveryTime = order.EstimatedDeliveryTime,
                    UpdatedAt = DateTime.UtcNow
                };

                await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, notification);

                return Ok(new
                {
                    message = "Доставка начата",
                    estimatedDeliveryTime = order.EstimatedDeliveryTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/courier/complete-delivery/5
        [HttpPut("complete-delivery/{orderId}")]
        public async Task<IActionResult> CompleteDelivery(int orderId)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.AssignedCourierId == courierId);

                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                if (order.Status != "OnTheWay")
                {
                    return BadRequest(new { message = "Невозможно завершить доставку для этого заказа" });
                }

                order.Status = "Delivered";
                order.UpdatedAt = DateTime.UtcNow;

                // Добавляем в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = "Delivered",
                    ChangedById = courierId,
                    Notes = "Заказ доставлен"
                };

                _context.OrderStatusHistory.Add(statusHistory);

                // Обновляем статус курьера на "свободен"
                var courier = await _context.Users.FindAsync(courierId);
                if (courier != null)
                {
                    courier.IsAvailable = true;
                }

                await _context.SaveChangesAsync();

                // Уведомления
                var clientNotification = new
                {
                    OrderId = orderId,
                    Status = "Delivered",
                    Message = "Заказ доставлен! Спасибо за заказ!",
                    DeliveredAt = DateTime.UtcNow
                };

                var adminNotification = new
                {
                    OrderId = orderId,
                    CustomerName = order.CustomerName,
                    TotalAmount = order.TotalAmount,
                    DeliveredAt = DateTime.UtcNow,
                    CourierName = $"{courier.FirstName} {courier.LastName}"
                };

                await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, clientNotification);
                await _notificationService.NotifyNewOrderAsync(adminNotification);

                return Ok(new { message = "Доставка завершена успешно" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/courier/availability
        [HttpPut("availability")]
        public async Task<IActionResult> UpdateAvailability([FromBody] UpdateAvailabilityRequest request)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var courier = await _context.Users.FindAsync(courierId);
                if (courier == null)
                {
                    return Unauthorized();
                }

                courier.IsAvailable = request.IsAvailable;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = request.IsAvailable ?
                        "Статус изменен на 'доступен'" :
                        "Статус изменен на 'не доступен'",
                    isAvailable = courier.IsAvailable
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/courier/earnings
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                var deliveries = await _context.Orders
                    .Where(o => o.AssignedCourierId == courierId &&
                               o.Status == "Delivered" &&
                               o.UpdatedAt >= start &&
                               o.UpdatedAt <= end)
                    .OrderByDescending(o => o.UpdatedAt)
                    .Select(o => new
                    {
                        o.Id,
                        o.CustomerName,
                        o.TotalAmount,
                        Commission = o.TotalAmount * 0.1m, // 10% комиссия
                        DeliveredAt = o.UpdatedAt
                    })
                    .ToListAsync();

                var totalEarnings = deliveries.Sum(d => d.Commission);

                return Ok(new
                {
                    Period = new { Start = start, End = end },
                    TotalEarnings = totalEarnings,
                    TotalDeliveries = deliveries.Count,
                    Deliveries = deliveries
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class UpdateAvailabilityRequest
    {
        public bool IsAvailable { get; set; }
    }
}