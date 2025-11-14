using Microsoft.EntityFrameworkCore.Storage;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Infrastructure.Persistence;


namespace Services.PaymentService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PaymentDbContext _db;
        private IPaymentRepository? _payment;

        public UnitOfWork(PaymentDbContext db)
        {
            _db = db;
        }

        public IPaymentRepository Payment =>
            _payment ??= new PaymentRepository(_db);

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            return await _db.Database.BeginTransactionAsync(ct);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
