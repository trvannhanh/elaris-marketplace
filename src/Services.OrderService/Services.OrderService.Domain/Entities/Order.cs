namespace Services.OrderService.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

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
