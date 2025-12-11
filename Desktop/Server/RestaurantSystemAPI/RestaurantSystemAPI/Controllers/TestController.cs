using Microsoft.AspNetCore.Mvc;
using RestaurantSystemAPI.Data;

namespace RestaurantSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok(new { message = "API работает!" });
        }

        [HttpGet("database")]
        public IActionResult TestDatabase()
        {
            try
            {
                var canConnect = _context.Database.CanConnect();
                return Ok(new
                {
                    message = "База данных подключена успешно!",
                    canConnect = canConnect
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Ошибка подключения к базе данных",
                    error = ex.Message
                });
            }
        }
    }
}