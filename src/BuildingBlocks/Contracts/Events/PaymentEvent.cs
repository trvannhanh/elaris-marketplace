namespace BuildingBlocks.Contracts.Events
{
    public record PaymentCapturedEvent(
        Guid OrderId, 
        decimal Amount,
        string TransactionId,
        DateTime CapturedAt
    );

    public record PaymentCaptureFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime FailedAt
    );

    public record PaymentSucceededEvent(
        Guid OrderId, 
        decimal Amount,
        DateTime CompletedAt
    );

    public record PaymentFailedEvent(
        Guid OrderId, 
        decimal Amount, 
        string Reason, 
        DateTime FailedAt
    );

    public record RefundSucceededEvent(
        Guid OrderId,
        decimal Amount,
        string Reason,
        DateTime RefundedAt
    );

    public record RefundFailedEvent(
        Guid OrderId,
        decimal Amount,
        string Reason,
        DateTime FailedAt
    );


}
