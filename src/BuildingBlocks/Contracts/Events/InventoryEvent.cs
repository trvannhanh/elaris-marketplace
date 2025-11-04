namespace BuildingBlocks.Contracts.Events
{
    public record InventoryUpdatedEvent(
        Guid OrderId,
        string ProductId,
        int Quantity,
        DateTime Timestamp
    );
    public record InventoryFailedEvent(
        Guid OrderId,
        string ProductId,
        string Reason,
        DateTime Timestamp
    );
}
