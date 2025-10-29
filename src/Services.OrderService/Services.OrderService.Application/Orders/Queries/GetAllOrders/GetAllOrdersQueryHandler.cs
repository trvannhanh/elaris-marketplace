using MapsterMapper;
using MediatR;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Queries.GetAllOrders
{
    public class GetAllOrdersQueryHandler
        : IRequestHandler<GetAllOrdersQuery, IEnumerable<OrderResponse>>
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;

        public GetAllOrdersQueryHandler(IOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OrderResponse>> Handle(GetAllOrdersQuery request, CancellationToken ct)
        {
            var orders = await _repo.GetAllAsync(ct);
            return _mapper.Map<IEnumerable<OrderResponse>>(orders);
        }
    }
}
