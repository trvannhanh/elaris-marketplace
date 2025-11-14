using Grpc.Core;
using Microsoft.Extensions.Logging;
using Services.InventoryService;
using Services.OrderService.Application.Interfaces;
using static Services.InventoryService.InventoryService;


namespace Services.OrderService.Infrastructure.Services
{
    public class InventoryGrpcClient : IInventoryGrpcClient
    {
        private readonly InventoryServiceClient _client;
        private readonly ILogger<InventoryGrpcClient> _logger;

        public InventoryGrpcClient(
            InventoryServiceClient client,
            ILogger<InventoryGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public CheckStockResponse CheckStock(string productId, int quantity)
        {
            try
            {
                // SYNC CALL – NHANH, KHÔNG BLOCK
                var request = new CheckStockRequest
                {
                    ProductId = productId,
                    Quantity = quantity
                };

                var response = _client.CheckStock(request);
                return response;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "❌ gRPC CheckStock failed for {ProductId}", productId);
                throw new InvalidOperationException("Không thể kiểm tra tồn kho", ex);
            }
        }
    }
}
