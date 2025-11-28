using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Common.Models;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;


namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryList
{
    public class GetInventoryListQueryHandler
        : IRequestHandler<GetInventoryListQuery, PaginatedList<InventoryItemDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetInventoryListQueryHandler> _logger;

        public GetInventoryListQueryHandler(
            IUnitOfWork uow,
            ILogger<GetInventoryListQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaginatedList<InventoryItemDto>> Handle(
            GetInventoryListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Inventory.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(x => x.ProductId.Contains(request.Search));
            }

            if (!string.IsNullOrWhiteSpace(request.SellerId))
            {
                query = query.Where(x => x.SellerId == request.SellerId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (request.LowStock == true)
            {
                query = query.Where(x => x.Quantity <= x.LowStockThreshold && x.Quantity > 0);
            }
            else if (request.LowStock == false)
            {
                query = query.Where(x => x.Quantity == 0);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.ProductId)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Adapt<List<InventoryItemDto>>();

            return new PaginatedList<InventoryItemDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
