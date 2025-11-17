namespace BuildingBlocks.Contracts.Events
{
    public record OrderCreatedEvent(
        Guid OrderId,
        string UserId,
        decimal TotalPrice,
        DateTime CreatedAt,
        string Status,
        List<BasketItemEvent> Items
    );

    public record OrderStatusUpdatedEvent(
        Guid OrderId,
        string Status,
        DateTime UpdatedAt
    );

    public record OrderItemEntry(
        string ProductId,
        int Quantity
    );

    public record OrderCompletedEvent(
        Guid OrderId,
        DateTime CompletedAt
    );

    public record OrderCompleteFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime Timestamp
    );
}
