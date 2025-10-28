using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order, CancellationToken cancellationToken);
        //Task SaveChangesAsync();
    }
}
