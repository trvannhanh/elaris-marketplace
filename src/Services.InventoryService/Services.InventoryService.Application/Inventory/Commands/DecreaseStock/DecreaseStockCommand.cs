using MediatR;
using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Inventory.Commands.DecreaseStock
{
    public record DecreaseStockCommand(
        string ProductId,
        int Quantity,
        string UserId,
        string UserRole,
        string? Note
    ) : IRequest<InventoryItemDto>;
}
