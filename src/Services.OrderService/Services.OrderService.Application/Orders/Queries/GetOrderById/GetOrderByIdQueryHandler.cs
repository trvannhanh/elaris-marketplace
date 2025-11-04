using Mapster;
using MediatR;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Models;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
    {
        private readonly IOrderRepository _orderRepo;

        public GetOrderByIdQueryHandler(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _orderRepo.GetByIdAsync(request.Id, cancellationToken);
            return order?.Adapt<OrderDto>();
        }
    }
}
