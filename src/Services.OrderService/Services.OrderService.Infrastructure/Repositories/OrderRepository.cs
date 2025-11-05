using MassTransit;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;
using Services.OrderService.Infrastructure.Persistence;


namespace Services.OrderService.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _db;

        public OrderRepository(OrderDbContext db)
        {
            _db = db;
        }

        public Task<Order> AddAsync(Order order, CancellationToken cancellationToken)
        {
            _db.Orders.Add(order);
            return Task.FromResult(order);
        }

        public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Orders.ToListAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public IQueryable<Order> Query()
        {
            return _db.Orders
                .Include(o => o.Items) // ← BẮT BUỘC
                .AsNoTracking();
        }

        public async Task<int> CountAsync(IQueryable<Order> query, CancellationToken ct)
            => await query.CountAsync(ct);

        public async Task<List<Order>> PaginateAsync(
            IQueryable<Order> query, int page, int pageSize, CancellationToken ct)
            => await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
