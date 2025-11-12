using Grpc.Core;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Grpc.Services
{
    public class InventoryGrpcService : InventoryGrpc.InventoryGrpcBase
    {
        private readonly IInventoryRepository _repo;
        private readonly ILogger<InventoryGrpcService> _logger;

        public InventoryGrpcService(IInventoryRepository repo, ILogger<InventoryGrpcService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
        {
            // Kiểm tra tồn kho (repo trả bool HasStockAsync + lấy remaining via GetByProductIdAsync)
            var available = await _repo.HasStockAsync(request.ProductId, request.Quantity, context.CancellationToken);
            var item = await _repo.GetByProductIdAsync(request.ProductId, context.CancellationToken);

            _logger.LogInformation("gRPC: CheckStock for {ProductId} qty={Qty} => available={Available}", request.ProductId, request.Quantity, available);

            return new CheckStockResponse
            {
                Available = available,
                Remaining = item?.AvailableStock ?? 0
            };
        }
    }
}
