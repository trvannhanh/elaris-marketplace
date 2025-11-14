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
        private readonly IInventoryGrpcClient _inventoryClient;
        private readonly ILogger<CreateOrderCommandHandler> _logger;


        public CreateOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publishEndpoint, IInventoryGrpcClient inventoryClient, ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
            _inventoryClient = inventoryClient;
            _logger = logger;
        }

        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {

            //0.5 Kiểm tra tồn kho qua gRPC
            foreach (var item in request.Items)
            {
                var result = _inventoryClient.CheckStock(item.ProductId, item.Quantity);

                if (!result.InStock)
                {
                    _logger.LogWarning("❌ Out of stock: {ProductId}, only {Stock} left",
                        item.ProductId, result.AvailableStock);

                    throw new InvalidOperationException(
                        $"Sản phẩm {item.ProductId} chỉ còn {result.AvailableStock} trong kho");
                }

                _logger.LogInformation("✅ Stock OK for {ProductId}: {Available}", item.ProductId, result.AvailableStock);
            }



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



            // 1. Lưu Order vào DB
            await _orderRepository.AddAsync(order, cancellationToken);


            _logger.LogInformation("Order . Publishing event...");

            // 2. Tạo event
            var eventToPublish = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalPrice,
                order.CreatedAt,
                order.Status.ToString(),
                order.Items.Select(i => new BasketItemEvent(i.ProductId, i.Name, i.Price, i.Quantity)).ToList()
            );

            // 3. Publish → MassTransit sẽ tự lưu vào Outbox + DB transaction
            try
            {
                await _publishEndpoint.Publish(eventToPublish, cancellationToken);
                _logger.LogInformation("✅ OrderCreatedEvent published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish OrderCreatedEvent ");
            }

            // 4. SaveChanges → Lưu cả Order + OutboxMessage trong 1 transaction
            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Order {OrderId} created and OrderCreatedEvent published via Outbox", order.Id);

            return order;
        }
    }
}
