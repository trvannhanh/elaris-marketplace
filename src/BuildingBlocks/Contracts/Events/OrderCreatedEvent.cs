

namespace BuildingBlocks.Contracts.Events
{
    public record OrderCreatedEvent(
    Guid OrderId,
    string ProductId,
    decimal TotalPrice
    );
}
