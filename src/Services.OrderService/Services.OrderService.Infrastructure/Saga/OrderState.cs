using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.OrderService.Infrastructure.Saga
{
    // Lưu trạng thái hiện tại của 1 Saga instance (tức là 1 Order đang được xử lý)
    public class OrderState : SagaStateMachineInstance, ISagaVersion
    {
        // ID để correlate các event khác nhau cùng 1 đơn hàng (MassTransit yêu cầu)
        public Guid CorrelationId { get; set; }

        // Tên của trạng thái hiện tại (ví dụ: AwaitingInventory, Completed, Failed...)
        public string CurrentState { get; set; } = string.Empty;

        // ===== DỮ LIỆU NGHIỆP VỤ CỦA ĐƠN HÀNG =====
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }

        // Danh sách sản phẩm trong giỏ hàng (truyền từ OrderCreatedEvent)
        public List<BasketItemEvent> Items { get; set; } = new();

        // ===== CÁC MỐC THỜI GIAN =====
        public DateTime CreatedAt { get; set; }
        public DateTime? InventoryReservedAt { get; set; }
        public DateTime? InventoryDecreasedAt { get; set; }
        public DateTime? PaymentAuthorizedAt { get; set; }
        public DateTime? PaymentCapturedAt { get; set; }
        public DateTime? OrderCompletedAt { get; set; }
        public DateTime? OrderCanceledAt { get; set; }

        // ===== CÁC ID LIÊN QUAN ĐẾN TIMEOUT =====
        // Dùng để quản lý timeout event của Inventory và Payment
        public Guid? InventoryTimeoutId { get; set; }
        public Guid? PaymentTimeoutId { get; set; }
        public Guid? OrderTimeoutId { get; set; }

        // Version của Saga instance (MassTransit dùng để concurrency control)
        public int Version { get; set; }
    }
}
