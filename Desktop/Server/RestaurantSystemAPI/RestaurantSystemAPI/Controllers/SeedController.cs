using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("test-data")]
        public async Task<IActionResult> AddTestData()
        {
            try
            {
                // Проверяем, есть ли уже данные
                var existingCategories = await _context.MenuCategories.AnyAsync();
                if (existingCategories)
                {
                    return Ok(new { message = "Данные уже существуют!" });
                }

                // Добавляем категории
                var categories = new List<MenuCategory>
                {
                    new MenuCategory { Name = "Супы", Description = "Ароматные горячие супы", DisplayOrder = 1 },
                    new MenuCategory { Name = "Горячие блюда", Description = "Основные блюда", DisplayOrder = 2 },
                    new MenuCategory { Name = "Салаты", Description = "Свежие салаты", DisplayOrder = 3 },
                    new MenuCategory { Name = "Напитки", Description = "Прохладительные и горячие напитки", DisplayOrder = 4 },
                    new MenuCategory { Name = "Медовуха", Description = "Домашняя медовуха разных сортов", DisplayOrder = 5 }
                };

                await _context.MenuCategories.AddRangeAsync(categories);
                await _context.SaveChangesAsync();

                // Добавляем позиции меню
                var items = new List<MenuItem>
                {
                    new MenuItem { Name = "Борщ", Description = "Традиционный украинский борщ со сметаной", Price = 180.00m, CategoryId = 1, PreparationTime = 15 },
                    new MenuItem { Name = "Уха царская", Description = "Уха из красной рыбы с овощами", Price = 220.00m, CategoryId = 1, PreparationTime = 20 },
                    new MenuItem { Name = "Котлета по-киевски", Description = "Куриная котлета с маслом и зеленью", Price = 320.00m, CategoryId = 2, PreparationTime = 25 },
                    new MenuItem { Name = "Гречка с грибами", Description = "Гречневая каша с шампиньонами и луком", Price = 190.00m, CategoryId = 2, PreparationTime = 15 },
                    new MenuItem { Name = "Цезарь с курицей", Description = "Салат с листьями айсберг, курицей и соусом цезарь", Price = 240.00m, CategoryId = 3, PreparationTime = 10 },
                    new MenuItem { Name = "Морс клюквенный", Description = "Освежающий клюквенный морс", Price = 90.00m, CategoryId = 4, PreparationTime = 2 },
                    new MenuItem { Name = "Медовуха классическая", Description = "Традиционная медовуха крепостью 5%", Price = 150.00m, CategoryId = 5, PreparationTime = 1 }
                };

                await _context.MenuItems.AddRangeAsync(items);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Тестовые данные успешно добавлены!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-users")]
        public async Task<IActionResult> AddTestUsers()
        {
            try
            {
                // Проверяем, есть ли уже пользователи
                var existingUsers = await _context.Users.AnyAsync();
                if (existingUsers)
                {
                    return Ok(new { message = "Пользователи уже существуют!" });
                }

                // Хэшируем пароли (временные хэши)
                var users = new List<User>
                {
                    new User { Email = "admin@restaurant.ru", PasswordHash = "temp_hash_admin", FirstName = "Иван", LastName = "Петров", PhoneNumber = "+79161234567", Role = "Admin" },
                    new User { Email = "courier@restaurant.ru", PasswordHash = "temp_hash_courier", FirstName = "Алексей", LastName = "Сидоров", PhoneNumber = "+79161234568", Role = "Courier", VehicleType = "Велосипед", VehiclePlate = "BKE-001" },
                    new User { Email = "client@example.ru", PasswordHash = "temp_hash_client", FirstName = "Мария", LastName = "Иванова", PhoneNumber = "+79161234569", Role = "Client" }
                };

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Тестовые пользователи добавлены!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}