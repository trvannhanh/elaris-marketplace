namespace BuildingBlocks.Contracts.Events
{
    public record PaymentAuthorizedEvent(
        Guid OrderId,
        decimal Amount,
        DateTime CompletedAt
    );
    public record PaymentAuthorizeFailedEvent(
        Guid OrderId,
        decimal Amount,
        string Reason,
        DateTime FailedAt
    );

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

    public record PaymentVoidedEvent(
        Guid OrderId,
        string Reason,
        DateTime Timestamp
    );

    public record PaymentVoidFailedEvent(
        Guid OrderId,
        string Reason,
        DateTime FailedAt
    );

    public record PaymentRefundedEvent(
        Guid OrderId,
        decimal Amount,
        string Reason,
        DateTime RefundedAt
    );

    public record PaymentRefundFailedEvent(
        Guid OrderId,
        decimal Amount,
        string Reason,
        DateTime FailedAt
    );


}
