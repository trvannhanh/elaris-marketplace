namespace BuildingBlocks.Contracts.Events
{
    public record InventoryReservedEvent(
    Guid OrderId,
    List<OrderItemEntry> Items,
    DateTime Timestamp
    );

    public record InventoryReserveFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime Timestamp
    );

    public record InventoryUpdatedEvent(
        Guid OrderId,
        List<OrderItemEntry> Items,
        DateTime Timestamp
    );

    public record InventoryUpdateFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime Timestamp
    );
 
}
