

using BuildingBlocks.Contracts.DTOs;
using MapsterMapper;
using MediatR;
using Services.OrderService.Application.Common.Models;
using Services.OrderService.Application.Interfaces;

namespace Services.OrderService.Application.Orders.GetOrdersWithFilters
{
    public class GetOrdersWithFiltersQueryHandler: IRequestHandler<GetOrdersWithFiltersQuery, PaginatedList<OrderDto>>
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;

        public GetOrdersWithFiltersQueryHandler(IOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginatedList<OrderDto>> Handle(
            GetOrdersWithFiltersQuery q,
            CancellationToken cancellationToken)
        {
            var query = _repo.Query(); // IQueryable<Order>

            // Search theo UserId
            if (!string.IsNullOrEmpty(q.Search))
                query = query.Where(o => o.UserId.Contains(q.Search));

            // Filter theo UserId exact
            if (!string.IsNullOrEmpty(q.UserId))
                query = query.Where(o => o.UserId == q.UserId);

            // Sorting
            query = (q.SortBy?.ToLower(), q.SortDirection?.ToLower()) switch
            {
                ("price", "desc") => query.OrderByDescending(o => o.TotalPrice),
                ("price", _) => query.OrderBy(o => o.TotalPrice),

                ("createdat", "desc") => query.OrderByDescending(o => o.CreatedAt),
                ("createdat", _) => query.OrderBy(o => o.CreatedAt),

                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            // Pagination
            var totalCount = await _repo.CountAsync(query, cancellationToken);
            var items = await _repo.PaginateAsync(query, q.Page, q.PageSize, cancellationToken);

            var mappedItems = _mapper.Map<IEnumerable<OrderDto>>(items);

            return new PaginatedList<OrderDto>
            {
                Items = mappedItems,
                PageNumber = q.Page,
                PageSize = q.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
