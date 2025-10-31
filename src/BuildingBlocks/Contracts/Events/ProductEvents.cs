namespace BuildingBlocks.Contracts.Events;

public record ProductPriceUpdatedEvent(
    string ProductId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime UpdatedAt
);