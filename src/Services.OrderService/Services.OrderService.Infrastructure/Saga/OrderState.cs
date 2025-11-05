using MassTransit;

namespace Services.OrderService.Infrastructure.Saga
{
    public class OrderState : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }  // Required by MassTransit

        public string CurrentState { get; set; } = string.Empty;

        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }

        public int Version { get; set; }

        // Keep track of workflow timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? ReservedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
    }
}
