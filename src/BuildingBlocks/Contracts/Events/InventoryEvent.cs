namespace BuildingBlocks.Contracts.Events
{
    public record InventoryUpdatedEvent(Guid OrderId, DateTime UpdatedAt);
    public record InventoryFailedEvent(Guid OrderId, string Reason);
}
