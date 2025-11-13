using Microsoft.EntityFrameworkCore.Storage;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IInventoryRepository Inventory { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
