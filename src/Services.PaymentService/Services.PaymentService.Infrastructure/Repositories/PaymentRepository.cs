
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;
using Services.PaymentService.Infrastructure.Persistence;

namespace Services.PaymentService.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _db;

        public PaymentRepository(PaymentDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Payment payment, CancellationToken ct)
            => await _db.Payments.AddAsync(payment, ct);

        public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
            => _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        public Task SaveChangesAsync(CancellationToken ct)
            => _db.SaveChangesAsync(ct);
    }
}
