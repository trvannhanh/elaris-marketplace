

using MediatR;

namespace Services.InventoryService.Application.Inventory.Commands.UpdateStock
{
    public record UpdateStockCommand(string ProductId, int Quantity) : IRequest;
}
