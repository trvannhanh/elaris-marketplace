


using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
        Task<List<Payment>> GetByUserIdAsync(string userId, CancellationToken ct);
        IQueryable<Payment> GetQueryable();
        Task AddAsync(Payment payment, CancellationToken ct);
        Task UpdateAsync(Payment payment, CancellationToken ct);
        Task AddHistoryAsync(PaymentHistory history, CancellationToken ct);
        Task<List<PaymentHistory>> GetHistoryByPaymentIdAsync(Guid paymentId, CancellationToken ct);
        Task<List<Payment>> GetPendingPaymentsAsync(DateTime olderThan, CancellationToken ct);
        Task<List<Payment>> GetFailedPaymentsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct);
    }
}
