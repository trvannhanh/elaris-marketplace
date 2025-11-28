using MediatR;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryStatistics
{
    public record GetInventoryStatisticsQuery : IRequest<InventoryStatisticsDto>;

    public class InventoryStatisticsDto
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalReservedQuantity { get; set; }
        public decimal EstimatedInventoryValue { get; set; } // Nếu có giá từ Product Service
    }
}
