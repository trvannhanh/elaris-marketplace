using MapsterMapper;
using MediatR;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;

        public GetOrderByIdQueryHandler(IOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _repo.GetByIdAsync(request.Id, cancellationToken);

            if (order == null)
                return null;

            return _mapper.Map<OrderResponse>(order);
        }
    }
}
