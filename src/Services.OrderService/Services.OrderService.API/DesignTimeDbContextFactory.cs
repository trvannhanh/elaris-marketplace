using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Infrastructure.Persistence;

namespace Services.OrderService.API
{
    public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5433;Database=elaris_orderdb;Username=elaris;Password=elaris_pwd",
                o => o.EnableRetryOnFailure()
            );
            return new OrderDbContext(optionsBuilder.Options);
        }
    }
}
