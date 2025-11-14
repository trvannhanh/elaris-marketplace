

using Microsoft.EntityFrameworkCore.Storage;

namespace Services.PaymentService.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IPaymentRepository Payment { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
