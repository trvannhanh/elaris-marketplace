using Grpc.Core;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.PaymentService;
using static Services.PaymentService.PaymentService;

namespace Services.OrderService.Infrastructure.Services
{
    public class PaymentGrpcClient : IPaymentGrpcClient
    {
        private readonly PaymentServiceClient _client;
        private readonly ILogger<PaymentGrpcClient> _logger;

        public PaymentGrpcClient(PaymentServiceClient client, ILogger<PaymentGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public CheckCardResponse CheckCard(string userId, string cardToken, decimal amount)
        {
            try
            {
                var req = new CheckCardRequest
                {
                    UserId = userId ?? "",
                    CardToken = cardToken ?? "",
                    Amount = (double)amount
                };

                // Synchronous unary call (generated client may expose blocking overload)
                var resp = _client.CheckCard(req);
                return resp;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "❌ gRPC CheckCard failed for user {UserId}", userId);
                throw new InvalidOperationException("❌ Không thể kiểm tra thẻ", ex);
            }
        }
    }
}
