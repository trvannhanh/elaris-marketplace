using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.ChangeStatus
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

        public UpdateOrderStatusCommandHandler(IUnitOfWork uow, ILogger<UpdateOrderStatusCommandHandler> logger, IPublishEndpoint publisher)
        {
            _uow = uow;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
        {
            var order = await _uow.Order.GetByIdAsync(request.OrderId, ct);

            if (order == null)
                throw new KeyNotFoundException($"Order {request.OrderId} not found");

            // Update status based on transition rules
            switch (request.NewStatus)
            {
                case OrderStatus.Processing:
                    order.MarkProcessing();
                    break;
                case OrderStatus.Completed:
                    order.MarkCompleted();
                    order.CompletedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Failed:
                    order.MarkFailed();
                    break;
                case OrderStatus.Cancelled:
                    order.MarkCancelled();
                    order.CancelledAt = DateTime.UtcNow;
                    order.CancellReason = request.Note ?? "Cancelled by admin";
                    break;
                default:
                    throw new InvalidOperationException($"Invalid status transition to {request.NewStatus}");
            }

            await _uow.SaveChangesAsync(ct);

            await _publisher.Publish(
                new OrderStatusUpdatedEvent(order.Id, order.Status.ToString(), DateTime.UtcNow),
                ct
            );

            _logger.LogInformation("✅ Order {OrderId} status updated to {Status}", request.OrderId, request.NewStatus);

            return true;
        }
    }
}
