
using Grpc.Core;
using MediatR;
using Services.InventoryService.Application.Inventory.Queries.CheckProductsAvailability;
using static Services.InventoryService.InventoryService;




namespace Services.InventoryService.API.Grpc
{
    public class InventoryGrpcService : InventoryServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InventoryGrpcService> _logger;

        public InventoryGrpcService(IMediator mediator, ILogger<InventoryGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override Task<CheckStockResponse> CheckStock(
            CheckStockRequest request,
            ServerCallContext context)
        {
            // SYNC CALL - SIÊU NHANH, KHÔNG BLOCK THREAD
            var query = new CheckProductsAvailabilityQuery(request.ProductId, request.Quantity);
            var result = _mediator.Send(query, context.CancellationToken).GetAwaiter().GetResult();

            return Task.FromResult(new CheckStockResponse
            {
                InStock = result.InStock,
                AvailableStock = result.AvailableStock,
                Message = result.Message
            });
        }
    }
}
