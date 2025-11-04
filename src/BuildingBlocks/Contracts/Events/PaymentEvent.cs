namespace BuildingBlocks.Contracts.Events
{
    public record PaymentSucceededEvent(
        Guid OrderId,
        decimal Amount,
        string ProductId,
        int Quantity,
        DateTime PaidAt
    );

    public record PaymentFailedEvent(
    Guid OrderId,
    decimal Amount,
    string Reason,
    DateTime FailedAt
    );
}
