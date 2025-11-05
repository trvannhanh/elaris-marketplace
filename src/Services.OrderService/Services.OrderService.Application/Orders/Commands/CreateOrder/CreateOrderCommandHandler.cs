using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CreateOrderCommandHandler> _logger;


        public CreateOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publishEndpoint, ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
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


            _logger.LogInformation("Order . Publishing event...");

            var eventToPublish = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalPrice,
                order.CreatedAt,
                order.Status.ToString()
            );

            try
            {
                await _publishEndpoint.Publish(eventToPublish, cancellationToken);
                _logger.LogInformation("OrderCreatedEvent published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreatedEvent ");
            }


            //// Publish event → sẽ được lưu vào Outbox table nhờ MassTransit
            //await _publishEndpoint.Publish(new OrderCreatedEvent(
            //    order.Id,
            //    order.UserId,
            //    order.TotalPrice,
            //    order.CreatedAt,
            //    order.Status.ToString()
            //), cancellationToken);


            await _orderRepository.SaveChangesAsync(cancellationToken);

            return order;
        }
    }
}
