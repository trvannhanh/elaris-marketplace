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
    }
}
