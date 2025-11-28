using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Application.Common.Models;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.GetLowStockItems
{
    public class GetLowStockItemsQueryHandler
        : IRequestHandler<GetLowStockItemsQuery, PaginatedList<InventoryItemDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetLowStockItemsQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PaginatedList<InventoryItemDto>> Handle(
            GetLowStockItemsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Inventory.GetQueryable()
                .Where(x => x.Quantity <= x.LowStockThreshold && x.Quantity > 0);

            if (!string.IsNullOrWhiteSpace(request.SellerId))
            {
                query = query.Where(x => x.SellerId == request.SellerId);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.Quantity)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedList<InventoryItemDto>
            {
                Items = items.Adapt<List<InventoryItemDto>>(),
                TotalCount = total,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
