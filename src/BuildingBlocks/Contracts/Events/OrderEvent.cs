namespace BuildingBlocks.Contracts.Events
{
    public record OrderEvent(
        Guid OrderId,
        string ProductId,
        decimal TotalPrice,
        DateTime CreatedAt,
        int Quantity,
        string Status
    );
}
