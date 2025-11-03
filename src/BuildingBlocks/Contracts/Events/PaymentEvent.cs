namespace BuildingBlocks.Contracts.Events
{
    public record PaymentSucceededEvent(
        Guid OrderId,
        decimal Amount,
        DateTime PaidAt
    );

    public record PaymentFailedEvent(
    Guid OrderId,
    decimal Amount,
    string Reason,
    DateTime FailedAt
);
}
