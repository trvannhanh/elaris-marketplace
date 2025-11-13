

using Microsoft.EntityFrameworkCore.Storage;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.Persistence;

namespace Services.InventoryService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InventoryDbContext _db;
        private IInventoryRepository? _inventory;

        public UnitOfWork(InventoryDbContext db)
        {
            _db = db;
        }

        public IInventoryRepository Inventory =>
            _inventory ??= new InventoryRepository(_db, ConfigureHttpClient());

        private HttpClient ConfigureHttpClient()
        {
            var client = new HttpClient();
            // Có thể config base address, headers...
            client.BaseAddress = new Uri("http://orderservice:8080");
            return client;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            return await _db.Database.BeginTransactionAsync(ct);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
