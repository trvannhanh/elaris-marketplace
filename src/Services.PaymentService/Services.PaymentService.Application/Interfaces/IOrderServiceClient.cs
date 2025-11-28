

namespace Services.PaymentService.Application.Interfaces
{
    public interface IOrderServiceClient
    {
        Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct);
    }

}
