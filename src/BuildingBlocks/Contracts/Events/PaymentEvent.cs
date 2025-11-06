namespace BuildingBlocks.Contracts.Events
{
    public record PaymentSucceededEvent(
        Guid OrderId, 
        decimal Amount,
        List<BasketItemEvent> Items, 
        DateTime CompletedAt
    );

    public record PaymentFailedEvent(
        Guid OrderId, 
        decimal Amount, 
        string Reason, 
        DateTime FailedAt
    );
}
