using MediatR;
using Services.InventoryService.Application.DTOs;


namespace Services.InventoryService.Application.Inventory.Commands.IncreaseStock
{
    public record IncreaseStockCommand(
        string ProductId,
        int Quantity,
        string UserId,
        string UserRole,
        string? Note
    ) : IRequest<InventoryItemDto>;
}
