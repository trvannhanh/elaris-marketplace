

using MediatR;
using Services.InventoryService.Application.Common.Models;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryHistory
{
    public record GetInventoryHistoryQuery(
    string ProductId,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 10) : IRequest<PaginatedList<InventoryHistoryDto>>;



    public class InventoryHistoryDto
    {
        public Guid Id { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public int QuantityChanged { get; set; }
        public string? ChangedBy { get; set; }
        public Guid? OrderId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
