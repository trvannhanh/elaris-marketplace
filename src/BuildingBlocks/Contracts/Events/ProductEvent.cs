namespace BuildingBlocks.Contracts.Events
{
    public record ProductCreatedEvent(
        string ProductId,
        string SellerId,
        //string Name,
        int Quantity,
        int LowStockThreshold,
        //decimal Price,
        DateTime CreatedAt
    );

    public record ProductPriceUpdatedEvent(
        string ProductId,
        decimal OldPrice,
        decimal NewPrice,
        DateTime UpdatedAt
    );

    public record ProductUpdatedEvent(
        string Id,
        string Name,
        string Description,
        decimal Price,
        DateTime UpdatedAt
    );

    public record ProductDeletedEvent(
        string Id,
        DateTime DeletedAt
    );
}
