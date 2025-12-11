using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;
using RestaurantSystemAPI.Models.DTOs.Orders; // Используем существующие DTO
using System.Security.Claims;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/client/[controller]")]
    [Authorize(Roles = "Client")]
    public class ClientOrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientOrdersController> _logger;

        public ClientOrdersController(ApplicationDbContext context, ILogger<ClientOrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/client/ClientOrders/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetUserId();

                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                    .Include(o => o.AssignedCourier)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new OrderResponseDto
                    {
                        Id = o.Id,
                        Status = o.Status,
                        TotalAmount = o.TotalAmount,
                        DeliveryAddress = o.DeliveryAddress,
                        CustomerName = o.CustomerName,
                        CustomerPhone = o.CustomerPhone,
                        CustomerEmail = o.CustomerEmail,
                        SpecialInstructions = o.SpecialInstructions,
                        CreatedAt = o.CreatedAt,
                        EstimatedDeliveryTime = o.EstimatedDeliveryTime,
                        PaymentMethod = o.PaymentMethod,
                        AssignedCourierId = o.AssignedCourierId,
                        OrderItems = o.OrderItems.Select(i => new OrderItemResponseDto
                        {
                            Id = i.Id,
                            MenuItemId = i.MenuItemId,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказов пользователя");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        // GET: api/client/ClientOrders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var userId = GetUserId();

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.AssignedCourier)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                var orderDto = new OrderResponseDto
                {
                    Id = order.Id,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    DeliveryAddress = order.DeliveryAddress,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    CustomerEmail = order.CustomerEmail,
                    SpecialInstructions = order.SpecialInstructions,
                    CreatedAt = order.CreatedAt,
                    EstimatedDeliveryTime = order.EstimatedDeliveryTime,
                    PaymentMethod = order.PaymentMethod,
                    AssignedCourierId = order.AssignedCourierId,
                    OrderItems = order.OrderItems.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        MenuItemId = i.MenuItemId,
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении заказа {id}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        // POST: api/client/ClientOrders/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return Unauthorized(new { message = "Пользователь не найден" });

                // Валидация данных
                if (dto.OrderItems == null || dto.OrderItems.Count == 0)
                {
                    return BadRequest(new { message = "Заказ не содержит товаров" });
                }

                // Проверяем существование товаров
                var menuItemIds = dto.OrderItems.Select(x => x.MenuItemId).ToList();
                var existingItems = await _context.MenuItems
                    .Where(m => menuItemIds.Contains(m.Id))
                    .ToListAsync();

                if (existingItems.Count != menuItemIds.Count)
                {
                    var missingItems = menuItemIds.Except(existingItems.Select(m => m.Id)).ToList();
                    _logger.LogWarning($"Не найдены товары с ID: {string.Join(", ", missingItems)}");
                }

                // Создаем заказ
                var order = new Order
                {
                    UserId = userId,
                    Status = "Pending",
                    TotalAmount = dto.TotalAmount,
                    DeliveryAddress = dto.DeliveryAddress ?? "Самовывоз",
                    CustomerName = dto.CustomerName ?? $"{user.FirstName} {user.LastName}",
                    CustomerPhone = dto.CustomerPhone ?? user.PhoneNumber,
                    CustomerEmail = dto.CustomerEmail ?? user.Email,
                    SpecialInstructions = dto.SpecialInstructions,
                    PaymentMethod = dto.PaymentMethod ?? "cash",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Сохраняем, чтобы получить Id заказа

                // Добавляем товары в заказ
                foreach (var itemDto in dto.OrderItems)
                {
                    var menuItem = existingItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId);

                    // Если товар не найден в базе, используем данные из DTO
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = itemDto.MenuItemId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        ItemName = itemDto.ItemName ?? menuItem?.Name ?? "Товар"
                    };

                    _context.OrderItems.Add(orderItem);
                }

                // Пробуем добавить в историю статусов
                try
                {
                    var statusHistory = new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "Pending",
                        ChangedById = userId,
                        Notes = "Заказ создан",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.OrderStatusHistory.Add(statusHistory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось добавить запись в историю статусов");
                    // Продолжаем без истории
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Создан новый заказ #{order.Id} для пользователя {userId}");

                return Ok(new
                {
                    id = order.Id,
                    message = "Заказ успешно создан",
                    total = order.TotalAmount,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании заказа",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // PUT: api/client/ClientOrders/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var userId = GetUserId();

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                // Проверяем, можно ли отменить заказ
                if (order.Status != "Pending" && order.Status != "Confirmed")
                {
                    return BadRequest(new { message = "Нельзя отменить заказ на этой стадии" });
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Заказ отменен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отмене заказа {id}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        // GET: api/client/ClientOrders/{id}/status-history
        [HttpGet("{id}/status-history")]
        public async Task<IActionResult> GetStatusHistory(int id)
        {
            try
            {
                var userId = GetUserId();

                // Проверяем, что заказ принадлежит пользователю
                var orderExists = await _context.Orders
                    .AnyAsync(o => o.Id == id && o.UserId == userId);

                if (!orderExists)
                    return NotFound(new { message = "Заказ не найден" });

                // Пробуем получить историю статусов
                try
                {
                    var history = await _context.OrderStatusHistory
                        .Where(h => h.OrderId == id)
                        .OrderByDescending(h => h.CreatedAt)
                        .Select(h => new OrderStatusHistory
                        {
                            Id = h.Id,
                            Status = h.Status,
                            Notes = h.Notes,
                            CreatedAt = h.CreatedAt
                        })
                        .ToListAsync();

                    return Ok(history);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "История статусов недоступна");
                    return Ok(new List<OrderStatusHistory>()); // Возвращаем пустой список
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении истории статусов заказа {id}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        // PUT: api/client/ClientOrders/{id}/process-payment
        [HttpPut("{id}/process-payment")]
        public async Task<IActionResult> ProcessPayment(int id, [FromBody] ProcessPaymentDto dto)
        {
            try
            {
                var userId = GetUserId();

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                if (order.Status != "Pending")
                {
                    return BadRequest(new { message = "Заказ уже обработан" });
                }

                // Обновляем статус заказа
                order.Status = "Confirmed";
                order.PaymentMethod = dto.PaymentMethod ?? "card";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Оплата обработана",
                    orderId = order.Id,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке оплаты заказа {id}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }
            return int.Parse(userIdClaim);
        }
    }
}