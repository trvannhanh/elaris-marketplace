
using MediatR;
using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Inventory.Queries
{
    public record GetInventoryByProductIdQuery(string ProductId)
        : IRequest<InventoryResponse?>;
}
