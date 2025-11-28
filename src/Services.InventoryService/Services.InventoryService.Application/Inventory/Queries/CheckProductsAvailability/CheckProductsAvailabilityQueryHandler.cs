using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.CheckProductsAvailability
{
    public class CheckProductsAvailabilityQueryHandler
        : IRequestHandler<CheckProductsAvailabilityQuery, CheckAvailabilityResponse>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CheckProductsAvailabilityQueryHandler> _logger;

        public CheckProductsAvailabilityQueryHandler(
            IUnitOfWork uow,
            ILogger<CheckProductsAvailabilityQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<CheckAvailabilityResponse> Handle(
            CheckProductsAvailabilityQuery request,
            CancellationToken cancellationToken)
        {
            bool inStock = true;

            var inventory = await _uow.Inventory.GetByProductIdAsync(request.ProductId, cancellationToken);
            var available = inventory?.AvailableQuantity ?? 0;
            bool isAvailable = available >= request.Quantity;

            if (!isAvailable) inStock = false;

            _logger.LogInformation("Checked availability for product {ProductId}. Available: {inStock}",
                request.ProductId, inStock);

            return new CheckAvailabilityResponse
            {
                InStock = inStock,
                AvailableStock = available,
                Message = isAvailable ? $"Đủ lượng tồn kho sản phẩm yêu cầu (yêu cầu {request.Quantity}), tồn kho: {available}" : $"Chỉ còn {available} sản phẩm (yêu cầu {request.Quantity})"
            };
        }
    }
}
