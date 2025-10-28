using BuildingBlocks.Contracts.Events;
using MassTransit;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands
{
    public class CreateOrderCommandHandler
    {
        private readonly IOrderRepository _repository;
        private readonly IPublishEndpoint _publisher;

        public CreateOrderCommandHandler(IOrderRepository repository, IPublishEndpoint publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<Order> Handle(string productId, int quantity, decimal totalPrice)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Quantity = quantity,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();

            await _publisher.Publish(new OrderCreatedEvent(order.Id, order.ProductId, order.TotalPrice, order.CreatedAt));

            return order;
        }
    }
}
