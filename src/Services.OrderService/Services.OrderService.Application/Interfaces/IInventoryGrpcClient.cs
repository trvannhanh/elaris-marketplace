


using Services.InventoryService;

namespace Services.OrderService.Application.Interfaces
{
    public interface IInventoryGrpcClient
    {
        CheckStockResponse CheckStock(string productId, int quantity);
    }
}
