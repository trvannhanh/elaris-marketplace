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

        public PreAuthorizeResponse PreAuthorize(Guid orderId, decimal amount, string userId)
        {
            try
            {
                var request = new PreAuthorizeRequest
                {
                    OrderId = orderId.ToString(),
                    Amount = amount.ToString("F2"),  // Chuẩn hóa 2 chữ số
                    UserId = userId
                };

                // SYNC CALL - NHANH
                return _client.PreAuthorize(request);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC PreAuthorize failed for Order {OrderId}", orderId);
                throw new InvalidOperationException("Không thể tạm giữ thanh toán", ex);
            }
        }
    }
}
