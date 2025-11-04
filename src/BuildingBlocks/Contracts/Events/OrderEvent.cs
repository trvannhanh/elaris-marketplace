namespace BuildingBlocks.Contracts.Events
{
    public record OrderCreatedEvent(
        Guid OrderId,
        string ProductId,
        decimal TotalPrice,
        DateTime CreatedAt,
        int Quantity,
        string Status
    );

    public record OrderStatusUpdatedEvent(
        Guid OrderId,
        string Status,
        DateTime UpdatedAt
    );

    public record OrderStockAvailableEvent(
        Guid OrderId,
        string ProductId,
        int Quantity, 
        DateTime Timestamp
    );
    public record OrderStockRejectedEvent(Guid OrderId, string Reason, DateTime Timestamp);
}
