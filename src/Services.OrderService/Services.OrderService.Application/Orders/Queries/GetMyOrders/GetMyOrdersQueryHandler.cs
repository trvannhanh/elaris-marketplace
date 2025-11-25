
using MapsterMapper;
using MediatR;
using Services.OrderService.Application.Common.Models;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Orders.Queries.GetMyOrders
{
    public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, PaginatedList<OrderDto>>
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;

        public GetMyOrdersQueryHandler(IOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginatedList<OrderDto>> Handle(GetMyOrdersQuery q, CancellationToken ct)
        {
            var query = _repo.Query()
                .Where(o => o.UserId == q.UserId)
                .OrderByDescending(o => o.CreatedAt);

            var totalCount = await _repo.CountAsync(query, ct);
            var items = await _repo.PaginateAsync(query, q.Page, q.PageSize, ct);
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
