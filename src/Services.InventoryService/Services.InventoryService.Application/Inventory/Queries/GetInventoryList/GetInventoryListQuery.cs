using MediatR;
using Services.InventoryService.Application.Common.Models;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Domain.Entities;


namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryList
{
    public record GetInventoryListQuery(
        string? Search,
        string? SellerId,
        InventoryStatus? Status,
        bool? LowStock,
        int Page = 1,
        int PageSize = 10) : IRequest<PaginatedList<InventoryItemDto>>;
}
