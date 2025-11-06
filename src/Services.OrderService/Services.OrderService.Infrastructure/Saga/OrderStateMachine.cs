using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.OrderService.Infrastructure.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // ======== CÁC TRẠNG THÁI CỦA ĐƠN HÀNG ========
    public State AwaitingInventory { get; private set; } = null!;
    public State InventoryReserved { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State PaymentProcessed { get; private set; } = null!;
    public State Completing { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // ======== CÁC SỰ KIỆN (EVENTS) NHẬN TỪ CÁC SERVICE KHÁC ========
    public Event<OrderCreatedEvent> OrderCreated { get; private set; } = null!;
    public Event<OrderItemsReservedEvent> ItemsReserved { get; private set; } = null!;
    public Event<OrderStockRejectedEvent> StockRejected { get; private set; } = null!;
    public Event<PaymentSucceededEvent> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<InventoryUpdatedEvent> InventoryUpdated { get; private set; } = null!;
    public Event<InventoryFailedEvent> InventoryUpdateFailed { get; private set; } = null!;

    // ======== TIMEOUT EVENTS ========
    public Schedule<OrderState, InventoryTimeout> InventoryTimeout { get; private set; } = null!;
    public Schedule<OrderState, PaymentTimeout> PaymentTimeout { get; private set; } = null!;

    public OrderStateMachine()
    {
        // Xác định property nào lưu trạng thái hiện tại
        InstanceState(x => x.CurrentState);

        // ==== CORRELATION: nối các event cùng 1 Order thông qua OrderId ====
        Event(() => OrderCreated, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => ItemsReserved, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockRejected, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentSucceeded, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdated, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdateFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // ==== THIẾT LẬP TIMEOUT CHO INVENTORY VÀ PAYMENT ====
        Schedule(() => InventoryTimeout, x => x.InventoryTimeoutId, x =>
        {
            x.Delay = TimeSpan.FromSeconds(30); // nếu sau 30s chưa có phản hồi thì timeout
            x.Received = e => e.CorrelateById(ctx => ctx.Message.OrderId);
        });

        Schedule(() => PaymentTimeout, x => x.PaymentTimeoutId, x =>
        {
            x.Delay = TimeSpan.FromSeconds(45);
            x.Received = e => e.CorrelateById(ctx => ctx.Message.OrderId);
        });

        // ====== LUỒNG XỬ LÝ CHÍNH (STATE TRANSITION) ======

        // --- Khi đơn hàng mới được tạo ---
        Initially(
            When(OrderCreated)
                .Then(ctx =>
                {
                    // Lưu thông tin đơn hàng
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.TotalPrice = ctx.Message.TotalPrice;
                    ctx.Saga.Items = ctx.Message.Items;
                    ctx.Saga.CreatedAt = ctx.Message.CreatedAt;
                })
                // Hẹn giờ timeout cho inventory
                .Schedule(InventoryTimeout, ctx => new InventoryTimeout(ctx.Saga.OrderId))
                // Gửi lệnh đến Inventory Service để dự trữ hàng
                .Publish(ctx => new ReserveInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                .TransitionTo(AwaitingInventory) // chuyển sang trạng thái chờ inventory
        );

        // --- Khi đang chờ Inventory phản hồi ---
        During(AwaitingInventory,

            // ✅ Khi Inventory báo đã giữ hàng thành công
            When(ItemsReserved)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.ReservedAt = DateTime.UtcNow)
                .Schedule(PaymentTimeout, ctx => new PaymentTimeout(ctx.Saga.OrderId))
                .Publish(ctx => new AuthorizePaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, ctx.Saga.UserId))
                .TransitionTo(AwaitingPayment),

            // ❌ Khi Inventory báo thiếu hàng
            When(StockRejected)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .TransitionTo(Failed)
                .Finalize(),

            // ⏱ Khi quá thời gian chờ Inventory mà chưa có phản hồi
            When(InventoryTimeout.Received)
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory timeout"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // --- Khi đang chờ Payment ---
        During(AwaitingPayment,

            // ✅ Thanh toán thành công
            When(PaymentSucceeded)
                .Unschedule(PaymentTimeout)
                .Then(ctx => ctx.Saga.PaidAt = DateTime.UtcNow)
                // Báo Inventory trừ hàng thật
                .Publish(ctx => new ConfirmInventoryReservationCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                .TransitionTo(Completing),

            // ❌ Thanh toán thất bại
            When(PaymentFailed)
                .Unschedule(PaymentTimeout)
                .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                .Publish(ctx => new ReleaseInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .TransitionTo(Failed)
                .Finalize()
        );

        // --- Khi đang hoàn tất (trừ hàng thật, hoàn tất đơn) ---
        During(Completing,

            // ✅ Cập nhật kho thành công
            When(InventoryUpdated)
                .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                .Publish(ctx => new CompleteOrderCommand(ctx.Saga.OrderId))
                .TransitionTo(Completed)
                .Finalize(),

            // ❌ Trừ kho thất bại (refund lại tiền)
            When(InventoryUpdateFailed)
                .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                .Publish(ctx => new RefundPaymentCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory deduction failed"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // Khi đạt trạng thái Completed hoặc Failed thì Saga coi như xong
        SetCompletedWhenFinalized();
    }
}

// Định nghĩa event cho Schedule
public record InventoryTimeout(Guid OrderId);
public record PaymentTimeout(Guid OrderId);
