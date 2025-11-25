using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order, CancellationToken cancellationToken);
        Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        IQueryable<Order> Query();
        Task<int> CountAsync(IQueryable<Order> query, CancellationToken ct);
        Task<List<Order>> PaginateAsync(IQueryable<Order> query, int page, int pageSize, CancellationToken ct);

        Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken ct = default);
        Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);
        Task UpdateAsync(Order order, CancellationToken ct = default);
    }
}
