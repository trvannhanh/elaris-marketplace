using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Infrastructure.Persistence;

namespace Services.InventoryService.API
{
    public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5433;Database=elaris_inventorydb;Username=elaris;Password=elaris_pwd",
                o => o.EnableRetryOnFailure()
            );
            return new InventoryDbContext(optionsBuilder.Options);
        }
    }
}
