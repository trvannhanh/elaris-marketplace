

using Services.PaymentService;

namespace Services.OrderService.Application.Interfaces
{
    public interface IPaymentGrpcClient
    {
        CheckCardResponse CheckCard(string userId, string cardToken, decimal amount);
    }
}
