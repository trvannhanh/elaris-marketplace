


using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment, CancellationToken ct);
        Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    }
}
