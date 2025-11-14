

namespace BuildingBlocks.Contracts.Commands
{
    // Lệnh từ Saga → Inventory
    public record ReserveInventoryCommand(Guid OrderId, List<InventoryItemReserve> Items);
    public record ReleaseInventoryCommand(Guid OrderId, List<InventoryItemReserve> Items);
    public record ConfirmInventoryReservationCommand(Guid OrderId, List<InventoryItemReserve> Items);

    // Lệnh từ Saga → Payment
    public record CapturePaymentCommand(Guid OrderId, decimal Amount);
    public record AuthorizePaymentCommand(Guid OrderId, decimal Amount, string UserId);
    public record RefundPaymentCommand(Guid OrderId, string Reason);

    // Lệnh từ Saga → Order
    public record CompleteOrderCommand(Guid OrderId);
    public record CancelOrderCommand(Guid OrderId, string Reason);

    // DTO
    public record InventoryItemReserve(string ProductId, int Quantity);
}
