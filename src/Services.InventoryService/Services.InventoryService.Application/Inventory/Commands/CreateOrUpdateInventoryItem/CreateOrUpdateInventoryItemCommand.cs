

using MediatR;
using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem
{
    public record CreateOrUpdateInventoryItemCommand(
        string ProductId,
        int Quantity,
        int LowStockThreshold,
        string? SellerId
    ) : IRequest<InventoryItemDto>;
}
