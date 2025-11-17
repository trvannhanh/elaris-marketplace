using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.OrderService.Infrastructure.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // ======== CÁC TRẠNG THÁI CỦA ĐƠN HÀNG ========
    public State ReservingInventory { get; private set; } = null!; // Đang giữ hàng
    public State AuthorizingPayment { get; private set; } = null!; // Đang giữ tiền
    public State DecreasingInventory { get; private set; } = null!; // Đang giảm hàng
    public State CapturingPayment { get; private set; } = null!; // Đang trừ tiền
    public State CompletingOrder { get; private set; } = null!; // Đang hoàn thành đơn
    public State Completed { get; private set; } = null!; // Đã hoàn tất đơn
    public State Failed { get; private set; } = null!; // Đã hủy đơn
    //public State Refunding { get; private set; } = null!;

    // ======== CÁC SỰ KIỆN (EVENTS) NHẬN TỪ CÁC SERVICE KHÁC ========
    public Event<OrderCreatedEvent> OrderCreated { get; private set; } = null!; // Đơn được tạo
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; } = null!; // Hàng đã được giữ
    public Event<InventoryReserveFailedEvent> InventoryReserveFailed { get; private set; } = null!; // Hàng giữ thất bại
    public Event<PaymentAuthorizedEvent> PaymentAuthorized { get; private set; } = null!; // Tiền đã được giữ
    public Event<PaymentAuthorizeFailedEvent> PaymentAuthorizedFailed { get; private set; } = null!; // Tiền giữ thất bại
    public Event<InventoryUpdatedEvent> InventoryUpdated { get; private set; } = null!; // Kho đã giảm
    public Event<InventoryUpdateFailedEvent> InventoryUpdateFailed { get; private set; } = null!; // Kho giảm thất bại
    public Event<PaymentCapturedEvent> PaymentCaptured { get; private set; } = null!; // Tiền đã trừ
    public Event<PaymentCaptureFailedEvent> PaymentCaptureFailed { get; private set; } = null!; // Tiền trừ thất bại
    public Event<OrderCompletedEvent> OrderCompleted { get; private set; } = null!; // Đơn đã hoàn thành
    public Event<OrderCompleteFailedEvent> OrderCompleteFailed { get; private set; } = null!; // Đơn hoàn thành thất bại
    //public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; } = null!;
    //public Event<PaymentRefundFailedEvent> PaymentRefundFailed { get; private set; } = null!;

    // ======== TIMEOUT EVENTS ========
    public Schedule<OrderState, InventoryTimeout> InventoryTimeout { get; private set; } = null!; // Bấm giờ đợi Inventory phản hồi
    public Schedule<OrderState, PaymentTimeout> PaymentTimeout { get; private set; } = null!; // Bấm giờ đợi Payment phản hồi
    public Schedule<OrderState, OrderTimeout> OrderTimeout { get; private set; } = null!; // Bấm giờ đợi Order phản hồi

    public OrderStateMachine()
    {
        // Xác định property nào lưu trạng thái hiện tại
        InstanceState(x => x.CurrentState);

        // ==== CORRELATION: nối các event cùng 1 Order thông qua OrderId ====
        Event(() => OrderCreated, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryReserved, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryReserveFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentAuthorized, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentAuthorizedFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCaptured, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCaptureFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdated, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryUpdateFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderCompleteFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        //Event(() => PaymentRefunded, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        //Event(() => PaymentRefundFailed, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // ==== THIẾT LẬP TIMEOUT CHO INVENTORY, PAYMENT VÀ ORDER ====
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

        Schedule(() => OrderTimeout, x => x.OrderTimeoutId, x =>
        {
            x.Delay = TimeSpan.FromSeconds(60);
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
                // Gửi lệnh Authorize (giữ tiền)
                .Publish(ctx => new AuthorizePaymentCommand(
                        ctx.Saga.OrderId,
                        ctx.Saga.TotalPrice,
                        ctx.Saga.UserId
                    ))
                // Hẹn giờ timeout cho payment
                .Schedule(PaymentTimeout, ctx => new PaymentTimeout(ctx.Saga.OrderId))
                .TransitionTo(AuthorizingPayment) // chuyển sang trạng thái chờ payment authorize
        );

        // --- Khi đang chờ Payment phản hồi Authorize (giữ tiền) ---
        During(AuthorizingPayment,

            // ✅ Khi Payment báo đã Authorize thành công
            When(PaymentAuthorized)
                    .Unschedule(PaymentTimeout)
                    .Then(ctx => ctx.Saga.PaymentAuthorizedAt = DateTime.UtcNow)
                    // Gửi lệnh Reserve Inventory (giữ hàng)
                    .Publish(ctx => new ReserveInventoryCommand(
                        ctx.Saga.OrderId,
                        ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                    ))
                    // Thiết lập timeout cho Inventory
                    .Schedule(InventoryTimeout, ctx => new InventoryTimeout(ctx.Saga.OrderId))
                    // Chuyển đến trạng thái đợi Reserve từ Inventory
                    .TransitionTo(ReservingInventory),

            // ❌ Khi Payment báo Authorize (giữ tiền) thất bại
            When(PaymentAuthorizedFailed)
                    .Unschedule(PaymentTimeout)
                    .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                    // Gửi lệnh hủy đơn hàng
                    .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                    // Chuyển đến trạng thái kết thúc với Failed
                    .TransitionTo(Failed)
                    .Then(ctx => Console.WriteLine($"[Saga] Order {ctx.Saga.OrderId} failed: Payment Authorize Failed"))
                    .Finalize(),

            // ⏱ Khi quá thời gian chờ Payment mà chưa có phản hồi
            When(PaymentTimeout.Received)
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Payment timeout"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // --- Khi đã Authorize (giữ tiền) thành công, chờ kết quả Reserve (giữ hàng)  ---
        During(ReservingInventory,

            // ✅ Kho giữ hàng thành công
            When(InventoryReserved)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.InventoryReservedAt = DateTime.UtcNow)
                // Gửi lệnh xác nhận cập nhật Kho
                .Publish(ctx => new ConfirmInventoryReservationCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                // Bật Timeout cho Inventory
                .Schedule(InventoryTimeout, ctx => new InventoryTimeout(ctx.Saga.OrderId))
                // Chuyển đến trạng thái đợi xác nhận giảm tồn kho
                .TransitionTo(DecreasingInventory),

            // ❌ Kho giữ hàng thất bại
            When(InventoryReserveFailed)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Void Payment (nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(
                        ctx.Saga.OrderId,
                        ctx.Saga.TotalPrice,
                        ctx.Saga.UserId,
                        "Inventory Reserve Failed"
                    ))
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, ctx.Message.Reason))
                .TransitionTo(Failed)
                .Finalize(),

            // ⏱ Timeout Inventory
            When(InventoryTimeout.Received)
                // Gửi lệnh Void Payment (nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(
                        ctx.Saga.OrderId,
                        ctx.Saga.TotalPrice,
                        ctx.Saga.UserId,
                        "Inventory Timeout When Reserving"
                    ))
                //Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory Timeout When Reserving"))
                .TransitionTo(Failed)
                .Finalize()
        );


        // --- Khi Payment đã được authorize và Inventory đã giữ hàng, đang xác nhận trừ hàng thật ---
        During(DecreasingInventory,
            // ✅ Cập nhật kho thành công 
            When(InventoryUpdated)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.InventoryDecreasedAt = DateTime.UtcNow)
                // Khi inventory đã update thành công: capture tiền thật
                .Publish(ctx => new CapturePaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice))
                // Bật Timeout cho Payment
                .Schedule(PaymentTimeout, ctx => new PaymentTimeout(ctx.Saga.OrderId))
                // đợi PaymentCapturedEvent để complete order
                .TransitionTo(CapturingPayment),

            // ❌ Trừ kho thất bại 
            When(InventoryUpdateFailed)
                .Unschedule(InventoryTimeout)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Void Payment (nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, ctx.Saga.UserId, "Inventory update failed"))
                // Gửi lệnh Release Stock (nhả hàng)
                .Publish(ctx => new ReleaseInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory deduction failed"))
                .TransitionTo(Failed)
                .Finalize(),

            // ⏱ Timeout Inventory
            When(InventoryTimeout.Received)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Void Payment(nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, ctx.Saga.UserId, "Inventory update failed"))
                // Gửi lệnh Release Stock (nhả hàng)
                .Publish(ctx => new ReleaseInventoryCommand(
                    ctx.Saga.OrderId,
                    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                ))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Inventory deduction failed"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // Khi Cập nhật kho đã thành công, đang đợi Capture tiền thật
        During(CapturingPayment,

            // ✅ Capture payment thành công 
            When(PaymentCaptured)
                .Unschedule(PaymentTimeout)
                .Then(ctx => ctx.Saga.PaymentCapturedAt = DateTime.UtcNow)
                // Gửi lệnh kết thúc đơn hàng với trạng thái Completed
                .Publish(ctx => new CompleteOrderCommand(ctx.Saga.OrderId))
                .Schedule(OrderTimeout, ctx => new OrderTimeout(ctx.Saga.OrderId))
                .TransitionTo(CompletingOrder),

            // ❌ Capture payment thất bại
            When(PaymentCaptureFailed)
                .Unschedule(PaymentTimeout)
                // Gửi lệnh Void Payment(nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, ctx.Saga.UserId, "Payment capture failed"))
                // Gửi lệnh cộng lại hàng 

                //// Gửi lệnh Release Stock (nhả hàng)
                //.Publish(ctx => new ReleaseInventoryCommand(
                //    ctx.Saga.OrderId,
                //    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                //))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Payment capture failed"))
                .TransitionTo(Failed)
                .Finalize(),

            // ⏱ Timeout Payment
            When(PaymentTimeout.Received)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Void Payment(nhả tiền)
                .Publish(ctx => new VoidPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, ctx.Saga.UserId, "Payment capture timeout"))
                // Gửi lệnh cộng lại hàng 
                //// Gửi lệnh Release Stock (nhả hàng)
                //.Publish(ctx => new ReleaseInventoryCommand(
                //    ctx.Saga.OrderId,
                //    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                //))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Payment capture timeout"))
                .TransitionTo(Failed)
                .Finalize()
        );

        // Khi đã cập nhật kho và capture tiền thành công, đang đợi Order xác nhận hoàn thành
        During(CompletingOrder,

            // ✅ Order Complete thành công 
            When(OrderCompleted)
                .Unschedule(OrderTimeout)
                // Kết thúc với trạng thái Completed
                .Then(ctx => ctx.Saga.OrderCompletedAt = DateTime.UtcNow)
                .TransitionTo(Completed)
                .Finalize(),

            // ❌ Order Complete thất bại
            When(OrderCompleteFailed)
                .Unschedule(OrderTimeout)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Refund Payment(hoàn tiền)
                .Publish(ctx => new RefundPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, "Order complete failed"))
                // Gửi lệnh Cộng lại Stock (nhả hàng)
                //.Publish(ctx => new ReleaseInventoryCommand(
                //    ctx.Saga.OrderId,
                //    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                //))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Order complete failed"))
                .TransitionTo(Failed)
                .Finalize(),

            // ⏱ Timeout Payment
            When(OrderTimeout.Received)
                .Then(ctx => ctx.Saga.OrderCanceledAt = DateTime.UtcNow)
                // Gửi lệnh Refund Payment(hoàn tiền)
                .Publish(ctx => new RefundPaymentCommand(ctx.Saga.OrderId, ctx.Saga.TotalPrice, "Order complete timeout"))
                // Gửi lệnh Cộng lại Stock (nhả hàng)
                //.Publish(ctx => new ReleaseInventoryCommand(
                //    ctx.Saga.OrderId,
                //    ctx.Saga.Items.Select(i => new InventoryItemReserve(i.ProductId, i.Quantity)).ToList()
                //))
                // Gửi lệnh hủy đơn hàng và kết thúc với Failed
                .Publish(ctx => new CancelOrderCommand(ctx.Saga.OrderId, "Order complete timeout"))
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
public record OrderTimeout(Guid OrderId);

