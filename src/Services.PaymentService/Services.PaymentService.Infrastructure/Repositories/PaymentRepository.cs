
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;
using Services.PaymentService.Infrastructure.Persistence;

namespace Services.PaymentService.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
        {
            return await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
        }

        public async Task<List<Payment>> GetByUserIdAsync(string userId, CancellationToken ct)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public IQueryable<Payment> GetQueryable()
        {
            return _context.Payments.AsNoTracking();
        }

        public async Task AddAsync(Payment payment, CancellationToken ct)
        {
            await _context.Payments.AddAsync(payment, ct);
        }

        public Task UpdateAsync(Payment payment, CancellationToken ct)
        {
            _context.Payments.Update(payment);

            return Task.CompletedTask;
        }

        public async Task AddHistoryAsync(PaymentHistory history, CancellationToken ct)
        {
            await _context.PaymentHistories.AddAsync(history, ct);
        }

        public async Task<List<PaymentHistory>> GetHistoryByPaymentIdAsync(
            Guid paymentId,
            CancellationToken ct)
        {
            return await _context.PaymentHistories
                .AsNoTracking()
                .Where(h => h.PaymentId == paymentId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Payment>> GetPendingPaymentsAsync(
            DateTime olderThan,
            CancellationToken ct)
        {
            return await _context.Payments
                .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < olderThan)
                .ToListAsync(ct);
        }

        public async Task<List<Payment>> GetFailedPaymentsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken ct)
        {
            var query = _context.Payments
                .Where(p => p.Status == PaymentStatus.Failed);

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= toDate.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

    }
}
