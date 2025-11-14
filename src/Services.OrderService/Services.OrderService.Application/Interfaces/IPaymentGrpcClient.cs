

using Services.PaymentService;

namespace Services.OrderService.Application.Interfaces
{
    public interface IPaymentGrpcClient
    {
        PreAuthorizeResponse PreAuthorize(Guid orderId, decimal amount, string userId);
    }
}
