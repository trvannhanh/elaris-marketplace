using MediatR;
using Services.InventoryService.Application.DTOs;


namespace Services.InventoryService.Application.Inventory.Commands.SetLowStockThreshold
{
    public record SetLowStockThresholdCommand(
        string ProductId,
        int Threshold,
        string UserId,
        string UserRole
    ) : IRequest<InventoryItemDto>;
}
