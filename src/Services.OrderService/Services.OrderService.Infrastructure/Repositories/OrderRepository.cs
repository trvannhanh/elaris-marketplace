﻿using Microsoft.EntityFrameworkCore;
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

        public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken)
        {
            _db.Orders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Orders.ToListAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public IQueryable<Order> Query() => _db.Orders.AsQueryable();

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
