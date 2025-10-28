using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPublishEndpoint _publisher;

        public CreateOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publisher)
        {
            _orderRepository = orderRepository;
            _publisher = publisher;
        }

        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                TotalPrice = request.TotalPrice,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order, cancellationToken);

            await _publisher.Publish(new OrderCreatedEvent(order.Id, order.ProductId, order.TotalPrice, order.CreatedAt), cancellationToken);

            return order;
        }
    }
}
