namespace BuildingBlocks.Contracts.Events
{
    public record PaymentFailedEvent(
        Guid OrderId,
        string Reason
    );

    public record PaymentSucceededEvent(
        Guid OrderId,
        DateTime PaidAt
    );
}
