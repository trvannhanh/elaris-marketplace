using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);
        Task SaveChangesAsync();
    }
}
