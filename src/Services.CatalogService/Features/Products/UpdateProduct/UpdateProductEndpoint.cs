using Services.CatalogService.Data;
using Services.CatalogService.Models;
using MongoDB.Driver;
using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.CatalogService.Features.Products.UpdateProduct
{
    public static class UpdateProductEndpoint
    {
        public static void MapUpdateProduct(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/products/{id}", async (MongoContext db ,IPublishEndpoint publisher, string id, Product updated) =>
            {
                var existing = await db.Products.FindOneAsync(p => p.Id == id);
                if (existing == null) return Results.NotFound();

                updated.Id = existing.Id; // đảm bảo giữ nguyên ID
                updated.CreatedAt = existing.CreatedAt; // không thay đổi CreatedAt

                var result = await db.Products.ReplaceOneAsync(x => x.Id == id, updated);

                if (result.ModifiedCount > 0)
                {
                    var ev = new ProductUpdatedEvent(
                        updated.Id!,
                        updated.Name,
                        updated.Description,
                        updated.Price,
                        DateTime.UtcNow
                    );
                    await publisher.Publish(ev);
                    return Results.Ok(updated);
                }

                return Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update product by ID");
        }
    }
}
