using MediatR;
using System;
namespace Services.InventoryService.Application.Inventory.Commands.BulkUpdateInventory
{
    public record BulkUpdateInventoryCommand(
        List<BulkInventoryItem> Items
    ) : IRequest<BulkUpdateResult>;


    public class BulkInventoryItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class BulkUpdateResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
