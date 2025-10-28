using Microsoft.EntityFrameworkCore;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;
using Services.OrderService.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
