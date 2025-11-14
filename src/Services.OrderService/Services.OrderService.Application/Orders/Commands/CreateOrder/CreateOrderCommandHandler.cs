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
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IInventoryGrpcClient _inventoryClient;
        private readonly IPaymentGrpcClient _paymentClient;
        private readonly ILogger<CreateOrderCommandHandler> _logger;


        public CreateOrderCommandHandler(IUnitOfWork uow, IPublishEndpoint publishEndpoint, IInventoryGrpcClient inventoryClient, IPaymentGrpcClient paymentClient, ILogger<CreateOrderCommandHandler> logger)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
            _inventoryClient = inventoryClient;
            _paymentClient = paymentClient;
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


            //var paymentResult = _paymentClient.PreAuthorize(
            //    order.Id,
            //    order.TotalPrice,
            //    request.UserId
            //);

            //if (!paymentResult.Success)
            //{
            //    _logger.LogWarning("Thanh toán tạm giữ thất bại: {Message}", paymentResult.Message);
            //    throw new InvalidOperationException($"Thanh toán thất bại: {paymentResult.Message}");
            //}

            //_logger.LogInformation("Thanh toán tạm giữ thành công: {PaymentId}", paymentResult.PaymentId);

            // 1. Lưu Order vào DB
            await _uow.Order.AddAsync(order, cancellationToken);

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
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Order {OrderId} created and OrderCreatedEvent published via Outbox", order.Id);

            return order;
        }
    }
}
