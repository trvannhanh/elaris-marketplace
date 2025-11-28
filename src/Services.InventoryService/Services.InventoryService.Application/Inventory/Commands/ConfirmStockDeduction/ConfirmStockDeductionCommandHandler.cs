using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;


namespace Services.InventoryService.Application.Inventory.Commands.ConfirmStockDeduction
{
    public class ConfirmStockDeductionCommandHandler
    : IRequestHandler<ConfirmStockDeductionCommand, InventoryItemDto>
    {
        private readonly IInventoryService _service;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ConfirmStockDeductionCommandHandler> _logger;


        public ConfirmStockDeductionCommandHandler(
            IInventoryService service,
            IUnitOfWork uow,
            ILogger<ConfirmStockDeductionCommandHandler> logger)
        {
            _service = service;
            _uow = uow;
            _logger = logger;
        }

        public async Task<InventoryItemDto> Handle(
            ConfirmStockDeductionCommand request,
            CancellationToken cancellationToken)
        {
            var item = await _service.ConfirmReservationAsync(request.OrderId, request.ProductId, request.Quantity, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            return MapToDto(item);
        }

        private InventoryItemDto MapToDto(InventoryItem item)
        {
            return new InventoryItemDto
            {
                ProductId = item.ProductId,
                SellerId = item.SellerId,
                Quantity = item.Quantity,
                ReservedQuantity = item.ReservedQuantity,
                AvailableQuantity = item.AvailableQuantity,
                LowStockThreshold = item.LowStockThreshold,
                Status = item.Status.ToString(),
                LastRestockDate = item.LastRestockDate,
                UpdatedAt = item.UpdatedAt
            };
        }
    }
}
