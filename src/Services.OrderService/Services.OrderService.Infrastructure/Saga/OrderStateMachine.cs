using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.OrderService.Infrastructure.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // ======== CÁC TRẠNG THÁI CỦA ĐƠN HÀNG ========
    public State AwaitingInventory { get; private set; } = null!;
    public State InventoryReserved { get; private set; } = null!;
    public State PaymentProcessed { get; private set; } = null!;
    public State AwaitingCapturePayment { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State Refunding { get; private set; } = null!;

    // ======== CÁC SỰ KIỆN (EVENTS) NHẬN TỪ CÁC SERVICE KHÁC ========
    public Event<OrderCreatedEvent> OrderCreated { get; private set; } = null!;
    public Event<OrderItemsReservedEvent> ItemsReserved { get; private set; } = null!;
    public Event<OrderStockRejectedEvent> StockRejected { get; private set; } = null!;
    public Event<PaymentSucceededEvent> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<PaymentCapturedEvent> PaymentCaptured { get; private set; } = null!;
    public Event<PaymentCaptureFailedEvent> PaymentCaptureFailed { get; private set; } = null!;
    public Event<InventoryUpdatedEvent> InventoryUpdated { get; private set; } = null!;
    public Event<InventoryFailedEvent> InventoryUpdateFailed { get; private set; } = null!;
    public Event<RefundSucceededEvent> RefundSucceeded { get; private set; } = null!;
    public Event<RefundFailedEvent> RefundFailed { get; private set; } = null!;

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
        Event(() => PaymentCaptured, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCaptureFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdated, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdateFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => RefundSucceeded, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => RefundFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));

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
                    ctx.Saga.CorrelationId = ctx.Message.OrderId;
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.TotalPrice = ctx.Message.TotalPrice;
                    ctx.Saga.Items = ctx.Message.Items;
                    ctx.Saga.CreatedAt = ctx.Message.CreatedAt;
                })
                // Gửi lệnh đến Inventory Service để dự trữ hàng
                .Publish(ctx => new ReserveInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                // Hẹn giờ timeout cho inventory
                .Schedule(InventoryTimeout, ctx => new InventoryTimeout(ctx.Saga.OrderId))
                .TransitionTo(AwaitingInventory) // chuyển sang trạng thái chờ inventory
        );

        // --- Khi đang chờ Inventory phản hồi ---
        During(AwaitingInventory,

                // ✅ Khi Inventory báo đã giữ hàng thành công
            When(ItemsReserved)
                    .Unschedule(InventoryTimeout)
                    .Then(ctx => ctx.Saga.ReservedAt = DateTime.UtcNow)
                    // Gửi lệnh pre-authorize payment
                    .Publish(ctx => new AuthorizePaymentCommand(
                        ctx.Saga.OrderId,
                        ctx.Saga.TotalPrice,
                        ctx.Saga.UserId
                    ))
                    // Thiết lập timeout cho payment
                    .Schedule(PaymentTimeout, ctx => new PaymentTimeout(ctx.Saga.OrderId))
                    .TransitionTo(InventoryReserved),

            // ❌ Khi Inventory báo thiếu hàng
            When(StockRejected)
                    .Unschedule(InventoryTimeout)
                    .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                    .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                    .TransitionTo(Failed)
                    .Then(ctx => Console.WriteLine($"[Saga] Order {ctx.Saga.OrderId} failed: StockRejected"))
                    .Finalize(),

            // ⏱ Khi quá thời gian chờ Inventory mà chưa có phản hồi
            When(InventoryTimeout.Received)
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory timeout"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // --- Khi hàng đã được giữ thành công, chờ kết quả thanh toán (pre-authorize) ---
        During(InventoryReserved,

            // ✅ Thanh toán tạm giữ thành công
            When(PaymentSucceeded)
                .Unschedule(PaymentTimeout)
                .Then(ctx => ctx.Saga.PaidAt = DateTime.UtcNow)
                .TransitionTo(PaymentProcessed)
                // Sau khi payment pre-authorized xong, xác nhận trừ hàng thật (capture)
                .Publish(ctx => new ConfirmInventoryReservationCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                )),

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
                .Finalize(),

            // ⏱ Timeout payment
            When(PaymentTimeout.Received)
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Payment timeout"))
                .Publish(ctx => new ReleaseInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                .TransitionTo(Failed)
                .Finalize()
        );


        // --- Khi Payment đã được pre-authorize, đang xác nhận trừ hàng thật ---
        During(PaymentProcessed,

            // ✅ Cập nhật kho thành công 
            When(InventoryUpdated)
                // Khi inventory đã update thành công: capture tiền thật
                .Publish(ctx => new CapturePaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice))
                // đợi PaymentCapturedEvent để complete order
                .TransitionTo(AwaitingCapturePayment),

            // ❌ Trừ kho thất bại → Refund tiền + Cancel đơn
            When(InventoryUpdateFailed)
                .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                .Publish(ctx => new RefundPaymentCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory deduction failed"))
                .TransitionTo(Refunding)
        );

        // Khi Cập nhật kho đã thành công, đang đợi Capture tiền thật
        During(AwaitingCapturePayment,

            // ✅ Capture payment thành công 
            When(PaymentCaptured)
                .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                .Publish(ctx => new CompleteOrderCommand(ctx.Saga.OrderId))
                .TransitionTo(Completed)
                .Finalize(),

            // ❌ Capture paymen thất bại → Refund tiền + Cancel đơn
            When(PaymentCaptureFailed)
                .Then(ctx => ctx.Saga.CanceledAt = DateTime.UtcNow)
                .Publish(ctx => new RefundPaymentCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Payment capture failed"))
                .TransitionTo(Refunding)
        );

        During(Refunding,
            When(RefundSucceeded)
                .Then(ctx => Console.WriteLine($"[Saga] Refund succeeded for order {ctx.Saga.OrderId}"))
                .TransitionTo(Failed)
                .Finalize(),

            When(RefundFailed)
                .Then(ctx => Console.WriteLine($"[Saga] Refund failed for order {ctx.Saga.OrderId}, reason: {ctx.Message.Reason}"))
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
