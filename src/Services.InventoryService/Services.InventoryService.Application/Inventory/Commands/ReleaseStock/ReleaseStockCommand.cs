using MediatR;
using Services.InventoryService.Application.DTOs;


namespace Services.InventoryService.Application.Inventory.Commands.ReleaseStock
{
    public record ReleaseStockCommand(
        string ProductId,
        int Quantity,
        Guid OrderId
    ) : IRequest<InventoryItemDto>;
}
