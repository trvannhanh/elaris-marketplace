

namespace BuildingBlocks.Contracts.Events
{
    public record BasketCheckedOutEvent(
        string UserId,
        List<BasketItemEvent> Items
    );

    public record BasketItemEvent(
        string ProductId,
        string Name,
        decimal Price,
        int Quantity
    );
}
