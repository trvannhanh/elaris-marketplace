using Microsoft.EntityFrameworkCore.Storage;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Infrastructure.Persistence;


namespace Services.OrderService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OrderDbContext _db;
        private IOrderRepository? _order;

        public UnitOfWork(OrderDbContext db)
        {
            _db = db;
        }

        public IOrderRepository Order =>
            _order ??= new OrderRepository(_db);

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
