using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Infrastructure.Persistence
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    }
}
