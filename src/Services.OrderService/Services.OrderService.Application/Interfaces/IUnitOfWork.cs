using Microsoft.EntityFrameworkCore.Storage;

namespace Services.OrderService.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IOrderRepository Order { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
