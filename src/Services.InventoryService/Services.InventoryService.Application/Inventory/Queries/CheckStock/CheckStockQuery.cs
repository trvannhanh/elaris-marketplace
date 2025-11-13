using MediatR;

namespace Services.InventoryService.Application.Inventory.Queries.CheckStock
{
    public record CheckStockQuery(string ProductId, int Quantity)
    : IRequest<CheckStockResponse>;

    public record CheckStockResponse(bool InStock, int AvailableStock, string Message);
}
