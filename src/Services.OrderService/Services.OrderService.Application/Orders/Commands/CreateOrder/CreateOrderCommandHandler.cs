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
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalPrice = request.Items.Sum(x => x.Price * x.Quantity),
                Items = request.Items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _orderRepository.AddAsync(order, cancellationToken);

           

            // Publish event → sẽ được lưu vào Outbox table nhờ MassTransit
            await _eventPublisher.PublishOrderCreatedEvent(new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalPrice,
                order.CreatedAt,
                order.Status.ToString()
            ), cancellationToken);


            await _orderRepository.SaveChangesAsync(cancellationToken);

            return order;
        }
    }
}
