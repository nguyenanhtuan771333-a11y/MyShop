using Microsoft.EntityFrameworkCore;

namespace MyShop.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Recharge> Recharges { get; set; }
        public DbSet<SpinHistory> SpinHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin3011",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("My07082010"),
                Role = "Admin",
                DiamondBalance = 0,
                TotalRechargeCount = 0,
                CanSpin = false
            });
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public int DiamondBalance { get; set; }
        public string? ResetToken { get; set; }
        public int TotalRechargeCount { get; set; }
        public bool CanSpin { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameId { get; set; }
        public int DiamondAmount { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public User User { get; set; }
    }

    public class Recharge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public int DiamondReceived { get; set; }
        public string Status { get; set; }
        public User User { get; set; }
    }

    public class SpinHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Prize { get; set; }
        public DateTime SpinDate { get; set; }
        public User User { get; set; }
    }
}