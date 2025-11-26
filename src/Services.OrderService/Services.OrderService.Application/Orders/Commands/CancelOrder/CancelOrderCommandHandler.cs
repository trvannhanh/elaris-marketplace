using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OrderService.Application.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(IUnitOfWork uow, ILogger<CancelOrderCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken ct)
        {
            var order = await _uow.Order.GetByIdAsync(request.OrderId, ct);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                throw new KeyNotFoundException($"Order {request.OrderId} not found");
            }

            // Kiểm tra ownership (sẽ check thêm ở controller level với Admin bypass)
            if (order.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to cancel order {OrderId} owned by {OwnerId}",
                    request.UserId, request.OrderId, order.UserId);
                throw new UnauthorizedAccessException("You can only cancel your own orders");
            }

            // Chỉ được cancel khi Pending
            if (order.Status != Domain.Entities.OrderStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
            }

            order.MarkCancelled();
            order.CancelledAt = DateTime.UtcNow;
            order.CancellReason = request.Reason ?? "Cancelled by user";

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Order {OrderId} cancelled by user {UserId}", request.OrderId, request.UserId);

            return true;
        }
    }
}
