namespace BuildingBlocks.Contracts.Events
{
    public record InventoryUpdatedEvent(
        Guid OrderId,
        List<OrderItemEntry> Items,
        DateTime Timestamp
    );

    public record InventoryFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime Timestamp
    );
}
