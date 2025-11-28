using MediatR;
using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Inventory.Commands.ConfirmStockDeduction
{
    public record ConfirmStockDeductionCommand(
        string ProductId,
        int Quantity,
        Guid OrderId
    ) : IRequest<InventoryItemDto>;
}
