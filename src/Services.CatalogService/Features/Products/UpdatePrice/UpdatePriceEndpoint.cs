using BuildingBlocks.Contracts.Events;
using MassTransit;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.UpdatePrice
{
    public static class UpdatePriceEndpoint
    {
        public static void MapUpdatePrice(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/products/{id}/price", async (string id, decimal newPrice, MongoContext db, IPublishEndpoint publisher) =>
            {
                var product = await db.Products.Find(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync();
                if (product == null) return Results.NotFound();

                var old = product.Price;
                var update = Builders<Product>.Update.Set(p => p.Price, newPrice);
                var res = await db.Products.UpdateOneAsync(p => p.Id == id, update);

                if (res.ModifiedCount > 0)
                {
                    var ev = new ProductPriceUpdatedEvent(id, old, newPrice, DateTime.UtcNow);
                    await publisher.Publish(ev);
                    return Results.Ok(new { productId = id, oldPrice = old, newPrice });
                }

                return Results.BadRequest();
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdatePrice")
            .WithTags("Products")
            .WithSummary("Update product price and publish event");
        }
    }
}
