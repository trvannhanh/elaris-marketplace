

using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;
using static MassTransit.ValidationResultExtensions;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;
        private readonly IInventoryGrpcClient _inventory;
        private readonly IPaymentGrpcClient _payment;
        private readonly ICatalogServiceClient _catalog;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IUnitOfWork uow,
            IPublishEndpoint publisher,
            IInventoryGrpcClient inventory,
            IPaymentGrpcClient payment,
            ICatalogServiceClient catalog,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _uow = uow;
            _publisher = publisher;
            _inventory = inventory;
            _payment = payment;
            _catalog = catalog;
            _logger = logger;
        }

        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken ct)
        {
            var userId = request.UserId;

            // 1. Lấy dữ liệu sản phẩm từ CatalogService (tránh client fake giá)
            _logger.LogInformation(" ========= Getting Product {ProductId}... ", request.ProductId);
            var product = await _catalog.GetProductAsync(request.ProductId, ct);
            if (product == null)
            {
                _logger.LogWarning("❌ Product with Id {ProductId} does not exist", request.ProductId);
                throw new InvalidOperationException("Product does not exist");
            }

            _logger.LogInformation(" =========✅ Product {ProductId} exist Checking Stock ... ", request.ProductId);
            // 2. Kiểm tra tồn kho
            var stock = _inventory.CheckStock(request.ProductId, request.Quantity);
            if (!stock.InStock)
            {
                _logger.LogWarning("❌ Out of stock: {ProductId}, only {Stock} left",
                        request.ProductId, stock.AvailableStock);
                throw new InvalidOperationException($"Only {stock.AvailableStock} items remaining");
            }

            _logger.LogInformation(" =========✅ Product {ProductId} Check Stock OK, Calculating total price", request.ProductId);

            // 3. Tính giá thật
            var totalPrice = product.Price * request.Quantity;

            _logger.LogInformation(" =========✅ Total Price for user {UserId} is {totalPrice} (Product {ProductId}, Quantity {Quantity}). Checking Card...", userId, totalPrice, request.ProductId, request.Quantity);

            // 4. Kiểm tra card thanh toán qua gRPC (sync)
            // lấy cardToken từ request (client/UI phải gửi cardToken trong CreateOrderCommand)
            var cardToken = request.CardToken;
            var cardCheck = _payment.CheckCard(request.UserId, cardToken, totalPrice);

            // Kết quả kiểm tra Card
            if (!cardCheck.Valid || cardCheck.Blocked || !cardCheck.SufficientLimit)
            {
                var reason = cardCheck.Message ?? "Card invalid or insufficient";
                _logger.LogWarning("❌ Card check failed for user {UserId}: {Reason}", request.UserId, reason);
                throw new InvalidOperationException($"❌ Payment check failed: {reason}");
            }

            _logger.LogInformation(" =========✅ Checked card with token {CardToken} success, valid card. Creating Order....", request.CardToken);

            // 5. Tạo đơn hàng
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalPrice = totalPrice,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = request.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = request.Quantity
                    }
                }
            };

            await _uow.Order.AddAsync(order, ct);

            _logger.LogInformation(" ========= Created Order . Publishing OrderCreated event...");

            // 6. Publish sự kiện OrderCreatedEvent → Saga xử lý tiếp
            var eventToPublish = new OrderCreatedEvent(
                order.Id,
                userId,
                totalPrice,
                order.CreatedAt,
                order.Status.ToString(),
                new List<BasketItemEvent>
                {
                    new BasketItemEvent(
                        request.ProductId, product.Name, product.Price, request.Quantity
                    )
                }
            );

            try
            {
                await _publisher.Publish(eventToPublish, ct);
                _logger.LogInformation("✅ OrderCreatedEvent published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish OrderCreatedEvent ");
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Order {OrderId} created and OrderCreatedEvent published via Outbox", order.Id);

            return order;
        }
    }
}
