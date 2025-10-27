using BuildingBlocks.Contracts.Events;
using MassTransit;
using Services.OrderService.Data;
using Services.OrderService.Data.DTO;

namespace Services.OrderService.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => "OrderService running");
        app.MapHealthChecks("/health");
        
        app.MapPost("/orders", async (OrderInput input, OrderDbContext db, IPublishEndpoint publisher) =>
        {

            var order = new Order
            {
                Id = Guid.NewGuid(), // Tạo Id mới
                ProductId = input.ProductId,
                Quantity = input.Quantity,
                TotalPrice = input.TotalPrice,
                CreatedAt = input.CreatedAt
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Publish sự kiện với đối tượng anonymous
            await publisher.Publish(new OrderCreatedEvent(
                order.Id,
                order.ProductId,
                order.TotalPrice
            ));
            return Results.Created($"/api/orders/{order.Id}", order);
        });
    }
}