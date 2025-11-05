using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.OrderService.Infrastructure.Saga
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public State OrderCreated { get; private set; }
        public State StockReserved { get; private set; }
        public State PaymentSucceeded { get; private set; }
        public State Completed { get; private set; }
        public State Canceled { get; private set; }

        // Events
        public Event<OrderCreatedEvent> OrderCreatedEvent { get; private set; }
        public Event<OrderItemsReservedEvent> OrderItemsReservedEvent { get; private set; }
        public Event<OrderStockRejectedEvent> OrderStockRejectedEvent { get; private set; }
        public Event<PaymentSucceededEvent> PaymentSucceededEvent { get; private set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; private set; }
        public Event<InventoryUpdatedEvent> InventoryUpdatedEvent { get; private set; }
        public Event<InventoryFailedEvent> InventoryFailedEvent { get; private set; }

        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            ConfigureCorrelationIds();

            Initially(
                When(OrderCreatedEvent)
                    .Then(context =>
                    {
                        var msg = context.Message;
                        context.Saga.OrderId = msg.OrderId;
                        context.Saga.UserId = msg.UserId;
                        context.Saga.TotalPrice = msg.TotalPrice;
                        context.Saga.CreatedAt = msg.CreatedAt;
                    })
                    .TransitionTo(OrderCreated)
            );

            During(OrderCreated,
                When(OrderItemsReservedEvent)
                    .Then(ctx => ctx.Saga.ReservedAt = DateTime.UtcNow)
                    .TransitionTo(StockReserved),

                When(OrderStockRejectedEvent)
                    .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                    .TransitionTo(Canceled)
                    .Finalize()
            );

            During(StockReserved,
                When(PaymentSucceededEvent)
                    .Then(ctx => ctx.Saga.PaidAt = DateTime.UtcNow)
                    .TransitionTo(PaymentSucceeded),

                When(PaymentFailedEvent)
                    .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                    .TransitionTo(Canceled)
                    .Finalize()
            );

            During(PaymentSucceeded,
                When(InventoryUpdatedEvent)
                    .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                    .TransitionTo(Completed)
                    .Finalize(),

                When(InventoryFailedEvent)
                    .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                    .TransitionTo(Canceled)
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        private void ConfigureCorrelationIds()
        {
            Event(() => OrderCreatedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderItemsReservedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderStockRejectedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentSucceededEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => InventoryUpdatedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => InventoryFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        }
    }
}
