using Microsoft.EntityFrameworkCore;
using RestaurantSystemAPI.Models.DTOs.Orders;
using RestaurantSystemAPI.Models.Entities;

namespace RestaurantSystemAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ContentBlock> ContentBlocks { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка отношений и ограничений
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.AssignedCourier)
                .WithMany(u => u.AssignedOrders)
                .HasForeignKey(o => o.AssignedCourierId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}