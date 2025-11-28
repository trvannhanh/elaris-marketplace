

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Application.Common.Models;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryHistory
{
    public class GetInventoryHistoryQueryHandler
        : IRequestHandler<GetInventoryHistoryQuery, PaginatedList<InventoryHistoryDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetInventoryHistoryQueryHandler(IUnitOfWork uow)
        {
           _uow = uow;
        }

        public async Task<PaginatedList<InventoryHistoryDto>> Handle(
            GetInventoryHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Inventory.GetHistoryQueryable()
                .Where(h => h.ProductId == request.ProductId);

            if (request.FromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(h => h.CreatedAt <= request.ToDate.Value);

            var total = await query.CountAsync(cancellationToken);

            var histories = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = histories.Adapt<List<InventoryHistoryDto>>();

            return new PaginatedList<InventoryHistoryDto>
            {
                Items = dtos,
                TotalCount = total,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
