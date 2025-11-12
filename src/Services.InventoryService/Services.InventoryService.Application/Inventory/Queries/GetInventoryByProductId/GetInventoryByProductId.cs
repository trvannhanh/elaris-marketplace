
using MediatR;
using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryByProductId
{
    public record GetInventoryByProductIdQuery(string ProductId)
        : IRequest<InventoryResponse?>;
}
