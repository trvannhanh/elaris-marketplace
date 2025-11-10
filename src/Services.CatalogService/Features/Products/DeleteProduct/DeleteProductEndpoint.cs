using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.DeleteProduct
{
    public static class DeleteProductEndpoint
    {
        public static void MapDeleteProduct(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/products/{id}", async (MongoContext db, string id) =>
            {
                var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
                await db.Products.UpdateOneAsync(x => x.Id == id, update);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminOnly")
            .WithName("DeleteProduct")
            .WithTags("Products")
            .WithSummary("Soft delete product");
        }
    }
}
