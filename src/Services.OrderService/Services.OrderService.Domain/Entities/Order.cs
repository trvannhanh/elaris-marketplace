namespace Services.OrderService.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = default!;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        public string? CancellReason { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public List<OrderItem> Items { get; set; } = new();

        public void MarkProcessing()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException("Order must be Pending before Processing");

            Status = OrderStatus.Processing;
        }

        public void MarkCompleted()
        {
            if (Status != OrderStatus.Processing)
                throw new InvalidOperationException("Order not ready to complete");

            Status = OrderStatus.Completed;
        }

        public void MarkCancelled()
        {
            if (Status == OrderStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a completed order");

            Status = OrderStatus.Failed;
        }

        public void MarkFailed()
        {
            if (Status == OrderStatus.Completed)
                throw new InvalidOperationException("Cannot fail a completed order");

            Status = OrderStatus.Failed;
        }
    }



    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}
