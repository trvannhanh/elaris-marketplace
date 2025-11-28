using MediatR;
using Services.InventoryService.Application.Common.Models;
using Services.InventoryService.Application.DTOs;


namespace Services.InventoryService.Application.Inventory.Queries.GetLowStockItems
{
    public record GetLowStockItemsQuery(
        string? SellerId, 
        int Threshold = 10,
        int Page = 1,
        int PageSize = 10) : IRequest<PaginatedList<InventoryItemDto>>;
}
