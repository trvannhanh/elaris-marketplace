namespace BuildingBlocks.Contracts.Events
{
    public record ProductCreatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        DateTime CreatedAt
    );

    public record ProductPriceUpdatedEvent(
    string ProductId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime UpdatedAt
    );
}
