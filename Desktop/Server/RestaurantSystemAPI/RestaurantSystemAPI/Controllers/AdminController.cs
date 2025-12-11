using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;
using System.Security.Claims;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var totalUsers = await _context.Users.CountAsync();
                var availableCouriers = await _context.Users
                    .Where(u => u.Role == "Courier" && u.IsAvailable == true)
                    .CountAsync();

                var today = DateTime.UtcNow.Date;
                var todayOrders = await _context.Orders
                    .Where(o => o.CreatedAt.Date == today)
                    .CountAsync();

                var todayRevenue = await _context.Orders
                    .Where(o => o.CreatedAt.Date == today && o.Status == "Delivered")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                return Ok(new
                {
                    orders = new
                    {
                        total = totalOrders,
                        pending = pendingOrders,
                        today = todayOrders
                    },
                    users = new
                    {
                        total = totalUsers,
                        availableCouriers = availableCouriers
                    },
                    revenue = new
                    {
                        today = todayRevenue
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/admin/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.AssignedCourier)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        o.Id,
                        o.Status,
                        o.TotalAmount,
                        CustomerName = o.CustomerName ?? "Не указано",
                        CustomerPhone = o.CustomerPhone ?? "Не указано",
                        CustomerEmail = o.CustomerEmail ?? "Не указано",
                        DeliveryAddress = o.DeliveryAddress ?? "Не указано",
                        o.CreatedAt,
                        o.EstimatedDeliveryTime,
                        Courier = o.AssignedCourier != null ? new
                        {
                            Id = o.AssignedCourier.Id,
                            FirstName = o.AssignedCourier.FirstName,
                            LastName = o.AssignedCourier.LastName,
                            PhoneNumber = o.AssignedCourier.PhoneNumber
                        } : null,
                        Items = o.OrderItems.Select(oi => new
                        {
                            Name = oi.ItemName,
                            oi.Quantity,
                            Price = oi.UnitPrice,
                            Total = oi.Quantity * oi.UnitPrice
                        }).ToList(),
                        ItemsCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.LastName)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Role,
                        u.PhoneNumber,
                        u.CreatedAt,
                        u.IsActive
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Проверка на существующего пользователя
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }

                var user = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Пользователь создан успешно",
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.Role,
                        user.PhoneNumber,
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Проверка email на уникальность (кроме текущего пользователя)
                if (request.Email != user.Email)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "Пользователь с таким email уже существует" });
                    }
                }

                user.Email = request.Email;
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.PhoneNumber = request.PhoneNumber;
                user.Role = request.Role;

                // Если указан новый пароль - хэшируем его
                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Пользователь обновлен успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Нельзя удалить самого себя
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (user.Id == currentUserId)
                {
                    return BadRequest(new { message = "Нельзя удалить свой аккаунт" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Пользователь удален успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/admin/couriers/available
        [HttpGet("couriers/available")]
        public async Task<IActionResult> GetAvailableCouriers()
        {
            try
            {
                var couriers = await _context.Users
                    .Where(u => u.Role == "Courier" && u.IsAvailable == true)
                    .Select(u => new
                    {
                        u.Id,
                        Name = $"{u.FirstName} {u.LastName}",
                        u.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(couriers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/admin/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var oldStatus = order.Status;
                order.Status = request.Status;

                // Если есть поле UpdatedAt - обновляем его
                var updatedAtProperty = typeof(Order).GetProperty("UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.SetValue(order, DateTime.UtcNow);
                }

                // Добавляем в историю статусов если такая таблица существует и есть ChangedAt поле
                try
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                    // Создаем историю без ChangedAt если его нет
                    var statusHistory = new OrderStatusHistory
                    {
                        OrderId = id,
                        Status = request.Status,
                        ChangedById = currentUserId,
                        Notes = request.Notes ?? $"Статус изменен с {oldStatus} на {request.Status}"
                    };

                    _context.OrderStatusHistory.Add(statusHistory);
                }
                catch
                {
                    // Если таблицы OrderStatusHistory нет или нет поля ChangedAt - просто игнорируем
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Статус заказа обновлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/admin/orders/{id}/assign-courier
        [HttpPut("orders/{id}/assign-courier")]
        public async Task<IActionResult> AssignCourierToOrder(int id, [FromBody] AssignCourierRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var courier = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.CourierId && u.Role == "Courier");

                if (courier == null)
                {
                    return BadRequest(new { message = "Курьер не найден" });
                }

                order.AssignedCourierId = request.CourierId;
                order.Status = "AssignedToCourier";

                // Если есть поле UpdatedAt - обновляем его
                var updatedAtProperty = typeof(Order).GetProperty("UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.SetValue(order, DateTime.UtcNow);
                }

                // Добавляем в историю статусов если такая таблица существует
                try
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                    // Создаем историю без ChangedAt если его нет
                    var statusHistory = new OrderStatusHistory
                    {
                        OrderId = id,
                        Status = "AssignedToCourier",
                        ChangedById = currentUserId,
                        Notes = $"Назначен курьер: {courier.FirstName} {courier.LastName}"
                    };

                    _context.OrderStatusHistory.Add(statusHistory);
                }
                catch
                {
                    // Если таблицы OrderStatusHistory нет или нет поля ChangedAt - просто игнорируем
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Курьер успешно назначен на заказ" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/admin/orders/{id}
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                // Проверяем наличие зависимых таблиц
                try
                {
                    // Сначала удаляем историю статусов если существует
                    var statusHistory = await _context.OrderStatusHistory
                        .Where(sh => sh.OrderId == id)
                        .ToListAsync();
                    if (statusHistory.Any())
                    {
                        _context.OrderStatusHistory.RemoveRange(statusHistory);
                    }
                }
                catch
                {
                    // Если таблицы нет - продолжаем
                }

                // Удаляем элементы заказа
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == id)
                    .ToListAsync();
                _context.OrderItems.RemoveRange(orderItems);

                // Удаляем сам заказ
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Заказ удален успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTO классы
    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }
    }

    public class OrderStatusUpdateRequest
    {
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class AssignCourierRequest
    {
        public int CourierId { get; set; }
    }
}