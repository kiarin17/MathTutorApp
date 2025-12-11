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
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ==================== РЕГИСТРАЦИЯ ====================

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.FirstName) ||
                    string.IsNullOrWhiteSpace(request.LastName))
                {
                    return BadRequest(new { message = "Все обязательные поля должны быть заполнены" });
                }

                // Проверяем, есть ли пользователь с таким email
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }

                // Хэшируем пароль
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Создаем пользователя
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = UserRole.Client, // По умолчанию регистрируем как клиента
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Генерируем JWT токен после регистрации
                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

                return Ok(new RegisterResponse
                {
                    Message = "Пользователь успешно зарегистрирован",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Register error: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера", error = ex.Message });
            }
        }

        // ==================== ВХОД ====================

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Email и пароль обязательны" });
                }

                // Находим пользователя
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    // Не говорим что пользователь не найден (безопасность)
                    return Unauthorized(new { message = "Неверный email или пароль" });
                }

                // Проверяем пароль
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    return Unauthorized(new { message = "Неверный email или пароль" });
                }

                // Генерируем JWT токен
                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

                // Возвращаем данные пользователя в зависимости от роли
                UserDto userDto = CreateUserDto(user);

                return Ok(new LoginResponse
                {
                    Message = "Вход выполнен успешно",
                    Token = token,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        // ==================== СПЕЦИАЛЬНАЯ РЕГИСТРАЦИЯ АДМИНА ====================

        [HttpPost("register-admin")]
        [Authorize(Roles = UserRole.Admin)] // Только админы могут создавать других админов
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = UserRole.Admin, // Сразу как админ
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Администратор успешно зарегистрирован",
                    user = CreateUserDto(user)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== СПЕЦИАЛЬНАЯ РЕГИСТРАЦИЯ КУРЬЕРА ====================

        [HttpPost("register-courier")]
        [Authorize(Roles = UserRole.Admin)] // Только админы могут создавать курьеров
        public async Task<IActionResult> RegisterCourier([FromBody] RegisterCourierRequest request)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Role = UserRole.Courier,
                    VehicleType = request.VehicleType,
                    VehiclePlate = request.VehiclePlate,
                    IsAvailable = true, // Новые курьеры сразу доступны
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Курьер успешно зарегистрирован",
                    user = CreateCourierDto(user)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== ПОЛУЧЕНИЕ ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ ====================

        [HttpGet("me")]
        [Authorize] // Любой авторизованный пользователь
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Получаем ID пользователя из токена
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == int.Parse(userId) && u.IsActive);

                if (user == null)
                {
                    return Unauthorized();
                }

                // Возвращаем разные данные в зависимости от роли
                if (user.Role == UserRole.Courier)
                {
                    return Ok(CreateCourierDto(user));
                }

                return Ok(CreateUserDto(user));
            }
            catch (Exception)
            {
                return Unauthorized();
            }
        }

        // ==================== АДМИН: ИЗМЕНЕНИЕ РОЛИ ПОЛЬЗОВАТЕЛЯ ====================

        [HttpPut("update-role/{userId}")]
        [Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Проверяем допустимые роли
                if (!UserRole.IsValidRole(request.Role))
                {
                    return BadRequest(new { message = "Недопустимая роль" });
                }

                // Нельзя изменить роль самого себя (админ не может разжаловать себя)
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                if (user.Id == currentUserId)
                {
                    return BadRequest(new { message = "Нельзя изменить свою собственную роль" });
                }

                user.Role = request.Role;

                // Если назначаем курьером, сбрасываем данные курьера
                if (request.Role == UserRole.Courier)
                {
                    user.VehicleType = null;
                    user.VehiclePlate = null;
                    user.IsAvailable = true;
                }
                else
                {
                    // Если убираем роль курьера, очищаем курьерские поля
                    user.VehicleType = null;
                    user.VehiclePlate = null;
                    user.IsAvailable = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Роль пользователя обновлена",
                    user = CreateUserDto(user)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== АДМИН: ВСЕ ПОЛЬЗОВАТЕЛИ ====================

        [HttpGet("users")]
        [Authorize(Roles = UserRole.Admin)]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // ==================== АДМИН: ВСЕ КУРЬЕРЫ ====================

        [HttpGet("couriers")]
        [Authorize(Roles = $"{UserRole.Admin},{UserRole.Courier}")]
        public async Task<IActionResult> GetCouriers()
        {
            var couriers = await _context.Users
                .Where(u => u.Role == UserRole.Courier && u.IsActive)
                .Select(u => new CourierDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    VehicleType = u.VehicleType,
                    VehiclePlate = u.VehiclePlate,
                    IsAvailable = u.IsAvailable ?? false,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(couriers);
        }

        // ==================== КУРЬЕР: ОБНОВИТЬ СТАТУС ДОСТУПНОСТИ ====================

        [HttpPut("courier/availability")]
        [Authorize(Roles = UserRole.Courier)]
        public async Task<IActionResult> UpdateCourierAvailability([FromBody] UpdateAvailabilityRequest request)
        {
            try
            {
                var courierId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                var courier = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == courierId && u.Role == UserRole.Courier);

                if (courier == null)
                {
                    return Unauthorized();
                }

                courier.IsAvailable = request.IsAvailable;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Статус доступности обновлен",
                    isAvailable = courier.IsAvailable
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== ПОЛЬЗОВАТЕЛЬ: ОБНОВИТЬ ПРОФИЛЬ ====================

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Unauthorized();
                }

                // Обновляем только разрешенные поля
                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

                // Курьеры могут обновлять свои данные транспорта
                if (user.Role == UserRole.Courier)
                {
                    user.VehicleType = request.VehicleType ?? user.VehicleType;
                    user.VehiclePlate = request.VehiclePlate ?? user.VehiclePlate;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Профиль успешно обновлен",
                    user = user.Role == UserRole.Courier ? CreateCourierDto(user) : CreateUserDto(user)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== СМЕНА ПАРОЛЯ ====================

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Unauthorized();
                }

                // Проверяем текущий пароль
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Текущий пароль неверен" });
                }

                // Хэшируем новый пароль
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Пароль успешно изменен" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private UserDto CreateUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        private CourierDto CreateCourierDto(User user)
        {
            return new CourierDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                VehicleType = user.VehicleType,
                VehiclePlate = user.VehiclePlate,
                IsAvailable = user.IsAvailable ?? false,
                CreatedAt = user.CreatedAt
            };
        }

        // ==================== ПРОВЕРОЧНЫЕ ENDPOINTS (для разработки) ====================

        [HttpGet("check-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAdmin()
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@restaurant.ru");

            if (admin == null)
            {
                return NotFound("Admin not found");
            }

            return Ok(new
            {
                admin.Email,
                admin.Role,
                admin.FirstName,
                admin.LastName,
                IsPasswordValid = BCrypt.Net.BCrypt.Verify("temp_hash_admin", admin.PasswordHash)
            });
        }

        [HttpPost("fix-admin-role")]
        [AllowAnonymous]
        public async Task<IActionResult> FixAdminRole()
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@restaurant.ru");

            if (admin == null)
            {
                return NotFound(new { message = "Админ не найден" });
            }

            admin.Role = UserRole.Admin;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Роль админа исправлена",
                user = new
                {
                    admin.Email,
                    admin.FirstName,
                    admin.LastName,
                    admin.Role
                }
            });
        }
    }

    // ==================== DTO КЛАССЫ ====================

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class RegisterCourierRequest : RegisterRequest
    {
        public string VehicleType { get; set; }
        public string VehiclePlate { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; }
    }


    public class UpdateProfileRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string VehicleType { get; set; }
        public string VehiclePlate { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class RegisterResponse
    {
        public string Message { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CourierDto : UserDto
    {
        public string VehicleType { get; set; }
        public string VehiclePlate { get; set; }
        public bool IsAvailable { get; set; }
    }
}