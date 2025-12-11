using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Data;

namespace TestProject1
{
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryDbContext(string dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? $"TestDb_{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}