

namespace BuildingBlocks.Contracts.Events
{
    public record ProductCreatedEvent(
        string ProductId,
        string Name,
        decimal Price,
        DateTime CreatedAt
    );
}
