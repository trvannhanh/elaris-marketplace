

using MediatR;

namespace Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem
{
    public record CreateOrUpdateInventoryItemCommand(string ProductId, int Stock) : IRequest;
}
