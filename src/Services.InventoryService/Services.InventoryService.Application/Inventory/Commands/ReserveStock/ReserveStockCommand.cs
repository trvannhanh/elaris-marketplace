using MediatR;
using Services.InventoryService.Application.DTOs;


namespace Services.InventoryService.Application.Inventory.Commands.ReserveStock
{
    public record ReserveStockCommand(
        string ProductId,
        int Quantity,
        Guid OrderId
    ) : IRequest<InventoryItemDto>;
}
