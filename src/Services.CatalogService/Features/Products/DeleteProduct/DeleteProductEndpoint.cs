using BuildingBlocks.Contracts.Events;
using MassTransit;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.DeleteProduct
{
    public static class DeleteProductEndpoint
    {
        public static void MapDeleteProduct(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/products/{id}", async (MongoContext db, IPublishEndpoint publisher, string id) =>
            {
                var existing = await db.Products.FindOneAsync(p => p.Id == id);
                if (existing == null) return Results.NotFound();

                var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
                var result = await db.Products.UpdateOneAsync(x => x.Id == id, update);

                if (result.ModifiedCount > 0)
                {
                    await publisher.Publish(new ProductDeletedEvent(id, DateTime.UtcNow));
                    return Results.NoContent();
                }

                return Results.BadRequest();
            })
            .RequireAuthorization("AdminOnly")
            .WithName("DeleteProduct")
            .WithTags("Products")
            .WithSummary("Soft delete product");
        }
    }
}
