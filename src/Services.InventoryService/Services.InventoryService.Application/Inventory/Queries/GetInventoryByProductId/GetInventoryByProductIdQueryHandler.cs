
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryByProductId
{
    public class GetInventoryByProductIdQueryHandler
        : IRequestHandler<GetInventoryByProductIdQuery, InventoryItemDto?>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetInventoryByProductIdQueryHandler> _logger;

        public GetInventoryByProductIdQueryHandler(
            IUnitOfWork uow,
            ILogger<GetInventoryByProductIdQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<InventoryItemDto?> Handle(
            GetInventoryByProductIdQuery request,
            CancellationToken cancellationToken)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(request.ProductId, cancellationToken);
            if (item == null)
            {
                _logger.LogWarning("Inventory not found for ProductId: {ProductId}", request.ProductId);
                return null;
            }

            return item.Adapt<InventoryItemDto>();
        }
    }
}
