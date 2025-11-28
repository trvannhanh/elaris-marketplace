

namespace Services.InventoryService.Domain.Entities
{
    public class StockReservation
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public ReservationStatus Status { get; set; }
    }


    public enum ReservationStatus
    {
        Active,
        Released,
        Confirmed,
        Expired
    }
}
