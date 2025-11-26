using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrderFromBasket
{
    public class CreateOrderFromBasketCommandHandler : IRequestHandler<CreateOrderFromBasketCommand, Order>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IInventoryGrpcClient _inventoryClient;
        private readonly IPaymentGrpcClient _paymentClient;
        private readonly ILogger<CreateOrderFromBasketCommandHandler> _logger;


        public CreateOrderFromBasketCommandHandler(IUnitOfWork uow, IPublishEndpoint publishEndpoint, IInventoryGrpcClient inventoryClient, IPaymentGrpcClient paymentClient, ILogger<CreateOrderFromBasketCommandHandler> logger)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
            _inventoryClient = inventoryClient;
            _paymentClient = paymentClient;
            _logger = logger;
        }

        public async Task<Order> Handle(CreateOrderFromBasketCommand request, CancellationToken cancellationToken)
        {

            //1. Kiểm tra tồn kho qua gRPC (sync)
            foreach (var item in request.Items)
            {
                var result = _inventoryClient.CheckStock(item.ProductId, item.Quantity);

                // Có item hết hàng
                if (!result.InStock)
                {
                    _logger.LogWarning("❌ Out of stock: {ProductId}, only {Stock} left",
                        item.ProductId, result.AvailableStock);

                    throw new InvalidOperationException(
                        $"Sản phẩm {item.ProductId} chỉ còn {result.AvailableStock} trong kho");
                }

                _logger.LogInformation("✅ Stock OK for {ProductId}: {Available}", item.ProductId, result.AvailableStock);
            }

            // 2. Kiểm tra card thanh toán qua gRPC (sync)
            // lấy cardToken từ request (client/UI phải gửi cardToken trong CreateOrderCommand)
            var cardToken = request.CardToken; 
            var cardCheck = _paymentClient.CheckCard(request.UserId, cardToken, request.TotalPrice);

            // Kết quả kiểm tra Card
            if (!cardCheck.Valid || cardCheck.Blocked || !cardCheck.SufficientLimit)
            {
                var reason = cardCheck.Message ?? "Card invalid or insufficient";
                _logger.LogWarning("❌ Card check failed for user {UserId}: {Reason}", request.UserId, reason);
                throw new InvalidOperationException($"❌ Payment check failed: {reason}");
            }

            // 3. Tạo Order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalPrice = request.TotalPrice,
                Items = request.Items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList()
            };

            // 3,5. Add Order
            await _uow.Order.AddAsync(order, cancellationToken);

            _logger.LogInformation(" ========= Created Order . Publishing OrderCreated event...");

            // 4. Tạo event OrderCreated 
            var eventToPublish = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalPrice,
                order.CreatedAt,
                order.Status.ToString(),
                order.Items.Select(i => new BasketItemEvent(i.ProductId, i.Name, i.Price, i.Quantity)).ToList()
            );

            // 4,5. Publish → MassTransit sẽ tự lưu vào Outbox + DB transaction
            try
            {
                await _publishEndpoint.Publish(eventToPublish, cancellationToken);
                _logger.LogInformation("✅ OrderCreatedEvent published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish OrderCreatedEvent ");
            }

            // 5. SaveChanges → Lưu cả Order + OutboxMessage trong 1 transaction
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Order {OrderId} created and OrderCreatedEvent published via Outbox", order.Id);

            return order;
        }
    }
}
