using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;
using RestaurantSystemAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/menu/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.MenuCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.DisplayOrder,
                        c.IsActive,
                        ItemsCount = c.MenuItems.Count(i => i.IsAvailable)
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/menu/all-items (для админа - все блюда включая неактивные)
        [HttpGet("all-items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllItemsForAdmin()
        {
            try
            {
                var items = await _context.MenuItems
                    .Include(i => i.Category)
                    .OrderBy(i => i.Category.DisplayOrder)
                    .ThenBy(i => i.Name)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Price,
                        i.ImageUrl,
                        i.PreparationTime,
                        i.Weight,
                        i.Composition,
                        i.IsAvailable,
                        i.IsPopular,
                        i.CreatedAt,
                        CategoryId = i.Category.Id,
                        CategoryName = i.Category.Name
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/menu/popular-items
        [HttpGet("popular-items")]
        public async Task<IActionResult> GetPopularItems()
        {
            try
            {
                var items = await _context.MenuItems
                    .Include(i => i.Category)
                    .Where(i => i.IsAvailable && i.IsPopular)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Price,
                        i.ImageUrl,
                        i.PreparationTime,
                        i.Weight,
                        i.Composition,
                        CategoryId = i.Category.Id,
                        CategoryName = i.Category.Name
                    })
                    .Take(10)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/menu/items/{id}/restore (восстановление блюда)
        [HttpPut("items/{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreMenuItem(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    return NotFound(new { message = "Блюдо не найдено" });
                }

                // Восстановление блюда
                menuItem.IsAvailable = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Блюдо восстановлено успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/menu/items
        [HttpGet("items")]
        public async Task<IActionResult> GetMenuItems()
        {
            try
            {
                var items = await _context.MenuItems
                    .Include(i => i.Category)
                    .Where(i => i.IsAvailable)
                    .OrderBy(i => i.Category.DisplayOrder)
                    .ThenBy(i => i.Name)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Price,
                        i.ImageUrl,
                        i.PreparationTime,
                        i.Weight,
                        i.Composition,
                        i.IsPopular,
                        i.CreatedAt,
                        CategoryId = i.Category.Id,
                        CategoryName = i.Category.Name
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/menu/items/by-category/{categoryId}
        [HttpGet("items/by-category/{categoryId}")]
        public async Task<IActionResult> GetItemsByCategory(int categoryId)
        {
            try
            {
                var categoryExists = await _context.MenuCategories
                    .AnyAsync(c => c.Id == categoryId && c.IsActive);

                if (!categoryExists)
                {
                    return NotFound(new { message = "Категория не найдена или неактивна" });
                }

                var items = await _context.MenuItems
                    .Where(i => i.IsAvailable && i.CategoryId == categoryId)
                    .OrderBy(i => i.Name)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Price,
                        i.ImageUrl,
                        i.PreparationTime,
                        i.Weight,
                        i.Composition,
                        i.IsPopular,
                        CategoryId = i.CategoryId,
                        CategoryName = i.Category.Name
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/menu/items/{id}
        [HttpGet("items/{id}")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            try
            {
                var item = await _context.MenuItems
                    .Include(i => i.Category)
                    .Where(i => i.IsAvailable)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Price,
                        i.ImageUrl,
                        i.PreparationTime,
                        i.Weight,
                        i.Composition,
                        i.IsPopular,
                        CategoryId = i.Category.Id,
                        CategoryName = i.Category.Name
                    })
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null)
                {
                    return NotFound(new { message = "Блюдо не найдено" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/menu/categories
        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверка на существующую категорию с таким именем
                var existingCategory = await _context.MenuCategories
                    .FirstOrDefaultAsync(c => c.Name == request.Name);

                if (existingCategory != null)
                {
                    return BadRequest(new { message = "Категория с таким названием уже существует" });
                }

                var category = new MenuCategory
                {
                    Name = request.Name,
                    Description = request.Description,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true
                };

                _context.MenuCategories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Категория создана успешно",
                    category = new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.DisplayOrder,
                        category.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/menu/categories/{id}
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var category = await _context.MenuCategories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Категория не найдена" });
                }

                // Проверка имени на уникальность (кроме текущей категории)
                if (request.Name != category.Name)
                {
                    var existingCategory = await _context.MenuCategories
                        .FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != id);

                    if (existingCategory != null)
                    {
                        return BadRequest(new { message = "Категория с таким названием уже существует" });
                    }
                }

                category.Name = request.Name;
                category.Description = request.Description;
                category.DisplayOrder = request.DisplayOrder;
                category.IsActive = request.IsActive;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Категория обновлена успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/menu/categories/{id}
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.MenuCategories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Категория не найдена" });
                }

                // Проверяем, есть ли активные блюда в этой категории
                var itemsCount = await _context.MenuItems
                    .CountAsync(i => i.CategoryId == id && i.IsAvailable);

                if (itemsCount > 0)
                {
                    return BadRequest(new
                    {
                        message = "Невозможно удалить категорию, так как в ней есть активные блюда",
                        itemsCount
                    });
                }

                // Деактивация категории
                category.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Категория деактивирована успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/menu/items
        [HttpPost("items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMenuItem([FromBody] CreateMenuItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверка на существующее блюдо с таким именем
                var existingItem = await _context.MenuItems
                    .FirstOrDefaultAsync(i => i.Name == request.Name);

                if (existingItem != null)
                {
                    return BadRequest(new { message = "Блюдо с таким названием уже существует" });
                }

                // Проверка существования категории
                var category = await _context.MenuCategories.FindAsync(request.CategoryId);
                if (category == null || !category.IsActive)
                {
                    return BadRequest(new { message = "Категория не найдена или неактивна" });
                }

                var menuItem = new MenuItem
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    ImageUrl = request.ImageUrl,
                    PreparationTime = request.PreparationTime,
                    Weight = request.Weight,
                    Composition = request.Composition,
                    CategoryId = request.CategoryId,
                    IsAvailable = true,
                    IsPopular = request.IsPopular,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Блюдо создано успешно",
                    item = new
                    {
                        menuItem.Id,
                        menuItem.Name,
                        menuItem.Description,
                        menuItem.Price,
                        menuItem.ImageUrl,
                        menuItem.PreparationTime,
                        menuItem.Weight,
                        menuItem.Composition,
                        menuItem.IsPopular,
                        CategoryId = menuItem.CategoryId,
                        CategoryName = category.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/menu/items/{id}
        [HttpPut("items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] UpdateMenuItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    return NotFound(new { message = "Блюдо не найдено" });
                }

                // Проверка имени на уникальность (кроме текущего блюда)
                if (request.Name != menuItem.Name)
                {
                    var existingItem = await _context.MenuItems
                        .FirstOrDefaultAsync(i => i.Name == request.Name && i.Id != id);

                    if (existingItem != null)
                    {
                        return BadRequest(new { message = "Блюдо с таким названием уже существует" });
                    }
                }

                // Проверка существования категории
                var category = await _context.MenuCategories.FindAsync(request.CategoryId);
                if (category == null || !category.IsActive)
                {
                    return BadRequest(new { message = "Категория не найдена или неактивна" });
                }

                // Сохраняем старое значение IsAvailable если оно не передано
                bool isAvailable = request.IsAvailable;
                if (!request.IsAvailable && menuItem.IsAvailable)
                {
                    // Если пытаются сделать неактивным, но оно было активным - оставляем активным
                    // Для деактивации используем DELETE endpoint
                    isAvailable = true;
                }

                menuItem.Name = request.Name;
                menuItem.Description = request.Description;
                menuItem.Price = request.Price;
                menuItem.ImageUrl = request.ImageUrl;
                menuItem.PreparationTime = request.PreparationTime;
                menuItem.Weight = request.Weight;
                menuItem.Composition = request.Composition;
                menuItem.CategoryId = request.CategoryId;
                menuItem.IsAvailable = isAvailable;
                menuItem.IsPopular = request.IsPopular;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Блюдо обновлено успешно",
                    item = new
                    {
                        menuItem.Id,
                        menuItem.Name,
                        menuItem.Description,
                        menuItem.Price,
                        menuItem.ImageUrl,
                        menuItem.PreparationTime,
                        menuItem.Weight,
                        menuItem.Composition,
                        menuItem.IsAvailable,
                        menuItem.IsPopular,
                        CategoryId = menuItem.CategoryId,
                        CategoryName = category.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/menu/items/{id}/toggle-popular
        [HttpPut("items/{id}/toggle-popular")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TogglePopular(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    return NotFound(new { message = "Блюдо не найдено" });
                }

                menuItem.IsPopular = !menuItem.IsPopular;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Блюдо {(menuItem.IsPopular ? "добавлено в" : "удалено из")} популярные",
                    isPopular = menuItem.IsPopular
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/menu/items/{id}
        [HttpDelete("items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    return NotFound(new { message = "Блюдо не найдено" });
                }

                // Деактивация блюда
                menuItem.IsAvailable = false;
                menuItem.IsPopular = false; // Убираем из популярных при деактивации
                await _context.SaveChangesAsync();

                return Ok(new { message = "Блюдо деактивировано успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTO классы
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100, ErrorMessage = "Название категории не должно превышать 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Порядок отображения обязателен")]
        [Range(0, 100, ErrorMessage = "Порядок отображения должен быть между 0 и 100")]
        public int DisplayOrder { get; set; }
    }

    public class UpdateCategoryRequest
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100, ErrorMessage = "Название категории не должно превышать 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Порядок отображения обязателен")]
        [Range(0, 100, ErrorMessage = "Порядок отображения должен быть между 0 и 100")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessage = "Статус активности обязателен")]
        public bool IsActive { get; set; }
    }

    public class CreateMenuItemRequest
    {
        [Required(ErrorMessage = "Название блюда обязательно")]
        [StringLength(100, ErrorMessage = "Название блюда не должно превышать 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 10000, ErrorMessage = "Цена должна быть между 0.01 и 10000")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "URL изображения не должен превышать 500 символов")]
        [Url(ErrorMessage = "Некорректный URL")]
        public string ImageUrl { get; set; }

        [Range(1, 300, ErrorMessage = "Время приготовления должно быть между 1 и 300 минутами")]
        public int? PreparationTime { get; set; }

        [Range(0.1, 5000, ErrorMessage = "Вес должен быть между 0.1 и 5000 граммами")]
        public double? Weight { get; set; }

        [StringLength(1000, ErrorMessage = "Состав не должен превышать 1000 символов")]
        public string Composition { get; set; }

        [Required(ErrorMessage = "ID категории обязателен")]
        public int CategoryId { get; set; }

        public bool IsPopular { get; set; } = false;
    }

    public class UpdateMenuItemRequest
    {
        [Required(ErrorMessage = "Название блюда обязательно")]
        [StringLength(100, ErrorMessage = "Название блюда не должно превышать 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 10000, ErrorMessage = "Цена должна быть между 0.01 и 10000")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "URL изображения не должен превышать 500 символов")]
        [Url(ErrorMessage = "Некорректный URL")]
        public string ImageUrl { get; set; }

        [Range(1, 300, ErrorMessage = "Время приготовления должно быть между 1 и 300 минутами")]
        public int? PreparationTime { get; set; }

        [Range(0.1, 5000, ErrorMessage = "Вес должен быть между 0.1 и 5000 граммами")]
        public double? Weight { get; set; }

        [StringLength(1000, ErrorMessage = "Состав не должен превышать 1000 символов")]
        public string Composition { get; set; }

        [Required(ErrorMessage = "ID категории обязателен")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Статус доступности обязателен")]
        public bool IsAvailable { get; set; }

        [Required(ErrorMessage = "Статус популярности обязателен")]
        public bool IsPopular { get; set; }
    }
}