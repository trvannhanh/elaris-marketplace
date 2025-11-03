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
        private readonly IEventPublisher _eventPublisher;


        public CreateOrderCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
        {
            _orderRepository = orderRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                TotalPrice = request.TotalPrice,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            await _orderRepository.AddAsync(order, cancellationToken);

            await _orderRepository.SaveChangesAsync(cancellationToken);

            // Publish event → sẽ được lưu vào Outbox table nhờ MassTransit
            await _eventPublisher.PublishOrderCreatedEvent(new OrderEvent(
                order.Id,
                order.ProductId,
                order.TotalPrice,
                order.CreatedAt,
                order.Quantity,
                order.Status.ToString()
            ), cancellationToken);

            

            return order;
        }
    }
}
