using Microsoft.EntityFrameworkCore;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Infrastructure.Persistence
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
        public DbSet<Order> Orders => Set<Order>();
    }
}
